import 'dart:async';
import 'dart:convert';
import 'dart:io';
import 'package:crypto/crypto.dart';
import 'package:dio/dio.dart';
import 'package:package_info_plus/package_info_plus.dart';
import 'package:path_provider/path_provider.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:signalr_netcore/signalr_client.dart';

import 'device_service.dart';
import 'install_service.dart';

enum UpdateState {
  idle,
  checking,
  available,
  downloading,
  downloaded,
  verifying,
  installing,
  restarting,
  healthy,
  failed,
  rollback,
}

class UpdateAgent {
  static final UpdateAgent _instance = UpdateAgent._internal();
  factory UpdateAgent() => _instance;
  UpdateAgent._internal();

  final Dio _dio = Dio();
  HubConnection? _hubConnection;
  Timer? _heartbeatTimer;
  Timer? _checkTimer;

  UpdateState _state = UpdateState.idle;
  String? _currentVersion;
  String? _lastHealthyVersion;
  String? _deviceId;
  String? _applicationId;
  String? _serverUrl;
  String? _updateChannel;

  UpdateState get state => _state;

  // ─── Initialize ────────────────────────────────────────────────────────────
  Future<void> initialize({
    required String serverUrl,
    required String applicationId,
    required String updateChannel,
  }) async {
    _serverUrl = serverUrl;
    _applicationId = applicationId;
    _updateChannel = updateChannel;

    final info = await PackageInfo.fromPlatform();
    _currentVersion = info.buildNumber;

    final prefs = await SharedPreferences.getInstance();
    _lastHealthyVersion = prefs.getString('last_healthy_version') ?? _currentVersion;

    // Register device
    _deviceId = await DeviceService.register(
      serverUrl: serverUrl,
      applicationId: applicationId,
      updateChannel: updateChannel,
      currentVersion: _currentVersion!,
    );

    // Connect to SignalR
    await _connectSignalR();

    // Start heartbeat every 30s
    _heartbeatTimer = Timer.periodic(
      const Duration(seconds: 30),
      (_) => _sendHeartbeat(),
    );

    // Poll for updates every 5 minutes (fallback)
    _checkTimer = Timer.periodic(
      const Duration(minutes: 5),
      (_) => checkForUpdate(),
    );

    // Mark as healthy if we just started successfully
    await _markHealthy();
  }

  // ─── SignalR ───────────────────────────────────────────────────────────────
  Future<void> _connectSignalR() async {
    final url = '$_serverUrl/hubs/deployment';
    _hubConnection = HubConnectionBuilder()
        .withUrl(url)
        .withAutomaticReconnect()
        .build();

    _hubConnection!.on('deployment.available', _onDeploymentAvailable);
    _hubConnection!.onreconnected((_) => _registerDeviceOnHub());

    try {
      await _hubConnection!.start();
      await _registerDeviceOnHub();
    } catch (e) {
      // Will retry via automatic reconnect
    }
  }

  Future<void> _registerDeviceOnHub() async {
    if (_deviceId != null) {
      await _hubConnection?.invoke('RegisterDevice', args: [_deviceId]);
      await _hubConnection?.invoke('JoinApplicationGroup', args: [_applicationId]);
    }
  }

  void _onDeploymentAvailable(List<Object?>? args) {
    final data = args?.first as Map<String, dynamic>?;
    if (data == null) return;
    final appId = data['applicationId'] as String?;
    if (appId != _applicationId) return;
    checkForUpdate();
  }

  // ─── Check for Update ──────────────────────────────────────────────────────
  Future<void> checkForUpdate() async {
    if (_state != UpdateState.idle && _state != UpdateState.healthy) return;
    _setState(UpdateState.checking);

    try {
      final res = await _dio.get(
        '$_serverUrl/api/v1/updates/latest',
        queryParameters: {
          'applicationId': _applicationId,
          'channel': _updateChannel,
          'currentVersion': _currentVersion,
        },
      );

      final data = res.data as Map<String, dynamic>;
      if (data['available'] == true) {
        _setState(UpdateState.available);
        await _downloadAndInstall(data);
      } else {
        _setState(UpdateState.healthy);
      }
    } catch (e) {
      _setState(UpdateState.failed);
      // Retry after 1 minute
      Timer(const Duration(minutes: 1), () {
        if (_state == UpdateState.failed) _setState(UpdateState.idle);
      });
    }
  }

  // ─── Download + Install ───────────────────────────────────────────────────
  Future<void> _downloadAndInstall(Map<String, dynamic> releaseData) async {
    final manifest = releaseData['manifest'] as Map<String, dynamic>?;
    if (manifest == null) {
      _setState(UpdateState.failed);
      return;
    }

    final artifactUrl = manifest['artifactUrl'] as String;
    final expectedSha256 = manifest['sha256'] as String;
    final version = releaseData['version'] as String;

    // Download
    _setState(UpdateState.downloading);
    final dir = await getTemporaryDirectory();
    final apkPath = '${dir.path}/update_$version.apk';

    try {
      await _dio.download(
        artifactUrl,
        apkPath,
        onReceiveProgress: (received, total) {
          if (total > 0) {
            final percent = (received / total * 100).toInt();
            _reportProgress(percent);
          }
        },
        options: Options(
          receiveTimeout: const Duration(minutes: 30),
        ),
      );
    } catch (e) {
      _setState(UpdateState.failed);
      return;
    }

    _setState(UpdateState.downloaded);

    // Verify SHA-256
    _setState(UpdateState.verifying);
    final file = File(apkPath);
    final bytes = await file.readAsBytes();
    final digest = sha256.convert(bytes);
    final actualSha256 = digest.toString();

    if (actualSha256 != expectedSha256) {
      await file.delete();
      _setState(UpdateState.failed);
      await _reportUpdateStatus('verification_failed');
      return;
    }

    // Install
    _setState(UpdateState.installing);
    await _reportUpdateStatus('installing');

    final success = await InstallService.installApk(apkPath);
    if (!success) {
      _setState(UpdateState.failed);
      await _reportUpdateStatus('install_failed');
      await _initiateRollback();
    }
  }

  // ─── Rollback ─────────────────────────────────────────────────────────────
  Future<void> _initiateRollback() async {
    if (_lastHealthyVersion == null || _lastHealthyVersion == _currentVersion) return;
    _setState(UpdateState.rollback);
    await _reportUpdateStatus('rollback');
    // Rollback is handled by re-installing the previous APK from cache or server
  }

  // ─── Health Reporting ─────────────────────────────────────────────────────
  Future<void> _markHealthy() async {
    final prefs = await SharedPreferences.getInstance();
    _lastHealthyVersion = _currentVersion;
    await prefs.setString('last_healthy_version', _currentVersion!);
    _setState(UpdateState.healthy);
    await _reportUpdateStatus('healthy');
  }

  Future<void> _reportUpdateStatus(String status) async {
    if (_deviceId == null) return;
    try {
      await _dio.post(
        '$_serverUrl/api/v1/devices/$_deviceId/update-status',
        data: {'status': status},
      );
    } catch (_) {}
  }

  Future<void> _reportProgress(int percent) async {
    if (_deviceId == null) return;
    try {
      await _dio.post(
        '$_serverUrl/api/v1/devices/$_deviceId/update-status',
        data: {'status': 'downloading', 'progress': percent},
      );
    } catch (_) {}
  }

  Future<void> _sendHeartbeat() async {
    if (_deviceId == null) return;
    try {
      await _dio.post(
        '$_serverUrl/api/v1/devices/heartbeat',
        data: {
          'deviceIdentifier': _deviceId,
          'appVersion': _currentVersion,
        },
      );
    } catch (_) {}
  }

  void _setState(UpdateState state) {
    _state = state;
  }

  void dispose() {
    _heartbeatTimer?.cancel();
    _checkTimer?.cancel();
    _hubConnection?.stop();
  }
}
