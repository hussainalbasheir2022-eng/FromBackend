import 'dart:async';
import 'dart:convert';
import 'dart:io';

import 'package:crypto/crypto.dart';
import 'package:device_info_plus/device_info_plus.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/services.dart';
import 'package:http/http.dart' as http;
import 'package:package_info_plus/package_info_plus.dart';
import 'package:path_provider/path_provider.dart';
import 'package:signalr_netcore/signalr_client.dart';

import 'config.dart';

class UpdateAgent {
  UpdateAgent._();
  static final UpdateAgent instance = UpdateAgent._();

  static const _install = MethodChannel('flutter_platform/install');

  Timer? _timer;
  bool _busy = false;
  String? _baseUrl;
  HubConnection? _hub;

  Future<void> start() async {
    await _resolveBaseUrl();
    await _register();
    await _connectHub();
    await check();
    _timer?.cancel();
    _timer = Timer.periodic(const Duration(seconds: 45), (_) => check());
  }

  Future<void> _resolveBaseUrl() async {
    for (final candidate in kPlatformBaseUrls) {
      try {
        final res = await http
            .get(Uri.parse('$candidate/api/v1/updates/latest').replace(queryParameters: {
              'applicationId': kApplicationId,
              'channel': kUpdateChannel,
              'currentBuildNumber': '0',
            }))
            .timeout(const Duration(seconds: 4));
        if (res.statusCode == 200) {
          _baseUrl = candidate;
          debugPrint('UpdateAgent using $candidate');
          return;
        }
      } catch (e) {
        debugPrint('UpdateAgent skip $candidate: $e');
      }
    }
    _baseUrl = kPlatformBaseUrls.first;
  }

  String get _api => _baseUrl ?? kPlatformBaseUrls.first;

  String _rewrite(String url) {
    final uri = Uri.parse(url);
    final base = Uri.parse(_api);
    return uri.replace(scheme: base.scheme, host: base.host, port: base.port).toString();
  }

  Future<String> _deviceId() async {
    final android = await DeviceInfoPlugin().androidInfo;
    final raw = android.id.isNotEmpty ? android.id : android.fingerprint;
    return '$kApplicationId|$raw';
  }

  Future<void> _connectHub() async {
    try {
      await _hub?.stop();
      final hub = HubConnectionBuilder()
          .withUrl('$_api/hubs/deployment')
          .withAutomaticReconnect()
          .build();
      hub.on('deployment.available', (args) {
        final data = args != null && args.isNotEmpty ? args.first : null;
        String? appId;
        if (data is Map) appId = data['applicationId']?.toString();
        if (appId != null && appId != kApplicationId) return;
        debugPrint('UpdateAgent SignalR: deployment.available');
        check();
      });
      await hub.start();
      await hub.invoke('JoinApplicationGroup', args: <Object>[kApplicationId]);
      _hub = hub;
      debugPrint('UpdateAgent SignalR connected for $kApplicationId');
    } catch (e) {
      debugPrint('UpdateAgent SignalR failed: $e');
    }
  }

  /// Flutter --split-per-abi stores ABI in versionCode (arm64 = 2000 + N).
  static int _logicalBuildNumber(int versionCode) =>
      versionCode >= 1000 ? versionCode % 1000 : versionCode;

  Future<void> _register() async {
    try {
      final info = await PackageInfo.fromPlatform();
      final android = await DeviceInfoPlugin().androidInfo;
      await http.post(
        Uri.parse('$_api/api/v1/devices/register'),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({
          'deviceIdentifier': await _deviceId(),
          'applicationId': kApplicationId,
          'deviceName': android.model,
          'osVersion': 'Android ${android.version.release}',
          'appVersion': '${_logicalBuildNumber(int.tryParse(info.buildNumber) ?? 0)}',
          'updateChannel': kUpdateChannel,
          'deviceModel': android.model,
          'manufacturer': android.manufacturer,
        }),
      );
    } catch (e) {
      debugPrint('UpdateAgent register failed: $e');
    }
  }

  Future<void> check() async {
    if (_busy) return;
    _busy = true;
    try {
      if (_baseUrl == null) await _resolveBaseUrl();
      final info = await PackageInfo.fromPlatform();
      final current = _logicalBuildNumber(int.tryParse(info.buildNumber) ?? 0);
      final uri = Uri.parse('$_api/api/v1/updates/latest').replace(queryParameters: {
        'applicationId': kApplicationId,
        'channel': kUpdateChannel,
        'currentBuildNumber': '$current',
        'currentVersion': info.buildNumber,
      });
      final res = await http.get(uri).timeout(const Duration(seconds: 15));
      if (res.statusCode != 200) return;
      final data = jsonDecode(res.body) as Map<String, dynamic>;
      if (data['available'] != true) {
        await _heartbeat('$current');
        return;
      }
      final manifest = data['manifest'] as Map<String, dynamic>?;
      if (manifest == null) return;
      final url = manifest['artifactUrl'] as String?;
      final sha = (manifest['sha256'] as String?)?.toLowerCase();
      if (url == null || sha == null) return;
      await _downloadAndInstall(_rewrite(url), sha);
    } catch (e) {
      debugPrint('UpdateAgent check failed: $e');
      _baseUrl = null;
    } finally {
      _busy = false;
    }
  }

  Future<void> _heartbeat(String version) async {
    try {
      await http.post(
        Uri.parse('$_api/api/v1/devices/heartbeat'),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({
          'deviceIdentifier': await _deviceId(),
          'appVersion': version,
        }),
      );
    } catch (_) {}
  }

  Future<void> _downloadAndInstall(String url, String expectedSha) async {
    final dir = await getTemporaryDirectory();
    final file = File('${dir.path}/ota.apk');
    final res = await http.get(Uri.parse(url)).timeout(const Duration(minutes: 5));
    if (res.statusCode != 200 || res.bodyBytes.isEmpty) return;
    final actual = sha256.convert(res.bodyBytes).toString();
    if (actual != expectedSha) return;
    await file.writeAsBytes(res.bodyBytes, flush: true);
    await _install.invokeMethod('installApk', {'apkPath': file.path});
  }
}
