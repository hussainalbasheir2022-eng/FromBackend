import 'dart:io';
import 'package:device_info_plus/device_info_plus.dart';
import 'package:dio/dio.dart';
import 'package:shared_preferences/shared_preferences.dart';

class DeviceService {
  static Future<String> register({
    required String serverUrl,
    required String applicationId,
    required String updateChannel,
    required String currentVersion,
  }) async {
    final prefs = await SharedPreferences.getInstance();

    // Return cached device ID if already registered
    final cached = prefs.getString('device_id');
    if (cached != null && cached.isNotEmpty) return cached;

    final info = await _getDeviceInfo();
    final dio = Dio();

    try {
      final res = await dio.post(
        '$serverUrl/api/v1/devices/register',
        data: {
          'deviceIdentifier': info['identifier'],
          'applicationId': applicationId,
          'deviceName': info['name'],
          'osVersion': info['osVersion'],
          'appVersion': currentVersion,
          'updateChannel': updateChannel,
          'deviceModel': info['model'],
          'manufacturer': info['manufacturer'],
        },
      );

      final deviceId = res.data['deviceId'] as String;
      await prefs.setString('device_id', deviceId);
      return deviceId;
    } catch (e) {
      // Return a local fallback identifier
      final fallback = info['identifier'] as String;
      await prefs.setString('device_id', fallback);
      return fallback;
    }
  }

  static Future<Map<String, String>> _getDeviceInfo() async {
    final plugin = DeviceInfoPlugin();
    if (Platform.isAndroid) {
      final android = await plugin.androidInfo;
      return {
        'identifier': android.id,
        'name': android.device,
        'model': android.model,
        'manufacturer': android.manufacturer,
        'osVersion': 'Android ${android.version.release}',
      };
    }
    return {
      'identifier': 'unknown-device',
      'name': 'Unknown Device',
      'model': 'Unknown',
      'manufacturer': 'Unknown',
      'osVersion': Platform.operatingSystemVersion,
    };
  }
}
