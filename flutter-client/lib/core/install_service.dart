import 'package:flutter/services.dart';

/// Handles APK installation on managed Android devices.
///
/// For Device Owner / Managed Device mode, this uses the
/// PackageInstaller API via a platform channel.
///
/// For non-managed devices in development, falls back to
/// the install_plugin which triggers the system installer dialog.
class InstallService {
  static const MethodChannel _channel =
      MethodChannel('flutter_platform/install');

  /// Installs the APK at [apkPath].
  ///
  /// On managed devices configured as Device Owner, this performs
  /// a silent installation without user interaction.
  ///
  /// On unmanaged devices, this shows the system install dialog.
  static Future<bool> installApk(String apkPath) async {
    try {
      final result = await _channel.invokeMethod<bool>(
        'installApk',
        {'apkPath': apkPath},
      );
      return result ?? false;
    } catch (e) {
      return false;
    }
  }

  /// Check if this device is a Device Owner managed device.
  static Future<bool> isDeviceOwner() async {
    try {
      final result = await _channel.invokeMethod<bool>('isDeviceOwner');
      return result ?? false;
    } catch (_) {
      return false;
    }
  }
}
