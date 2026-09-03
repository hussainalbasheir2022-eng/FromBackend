package com.flutterplatform.app

import android.app.admin.DevicePolicyManager
import android.content.Context
import android.content.Intent
import android.content.pm.PackageInstaller
import android.net.Uri
import android.os.Build
import androidx.core.content.FileProvider
import io.flutter.embedding.engine.plugins.FlutterPlugin
import io.flutter.plugin.common.MethodCall
import io.flutter.plugin.common.MethodChannel
import java.io.File
import java.io.FileInputStream

class InstallPlugin : FlutterPlugin, MethodChannel.MethodCallHandler {

    private lateinit var channel: MethodChannel
    private lateinit var context: Context

    override fun onAttachedToEngine(binding: FlutterPlugin.FlutterPluginBinding) {
        context = binding.applicationContext
        channel = MethodChannel(binding.binaryMessenger, "flutter_platform/install")
        channel.setMethodCallHandler(this)
    }

    override fun onDetachedFromEngine(binding: FlutterPlugin.FlutterPluginBinding) {
        channel.setMethodCallHandler(null)
    }

    override fun onMethodCall(call: MethodCall, result: MethodChannel.Result) {
        when (call.method) {
            "installApk" -> {
                val apkPath = call.argument<String>("apkPath") ?: run {
                    result.error("INVALID_ARG", "apkPath is required", null)
                    return
                }
                installApk(apkPath, result)
            }
            "isDeviceOwner" -> {
                val dpm = context.getSystemService(Context.DEVICE_POLICY_SERVICE) as DevicePolicyManager
                val packageName = context.packageName
                result.success(dpm.isDeviceOwnerApp(packageName))
            }
            else -> result.notImplemented()
        }
    }

    private fun installApk(apkPath: String, result: MethodChannel.Result) {
        val file = File(apkPath)
        if (!file.exists()) {
            result.error("FILE_NOT_FOUND", "APK file not found: $apkPath", null)
            return
        }

        val dpm = context.getSystemService(Context.DEVICE_POLICY_SERVICE) as DevicePolicyManager
        val isDeviceOwner = dpm.isDeviceOwnerApp(context.packageName)

        if (isDeviceOwner && Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP) {
            // ── Silent installation via PackageInstaller (Device Owner mode) ──
            silentInstall(file, result)
        } else {
            // ── Fallback: Show system install dialog ──
            showInstallDialog(file, result)
        }
    }

    private fun silentInstall(apkFile: File, result: MethodChannel.Result) {
        val packageInstaller = context.packageManager.packageInstaller
        val params = PackageInstaller.SessionParams(PackageInstaller.SessionParams.MODE_FULL_INSTALL)

        val sessionId = packageInstaller.createSession(params)
        val session = packageInstaller.openSession(sessionId)

        try {
            session.openWrite("package", 0, apkFile.length()).use { output ->
                FileInputStream(apkFile).use { input ->
                    input.copyTo(output)
                    session.fsync(output)
                }
            }

            val intent = Intent(context, InstallReceiver::class.java).apply {
                action = "com.flutterplatform.INSTALL_COMPLETE"
            }
            val pendingIntent = android.app.PendingIntent.getBroadcast(
                context, sessionId, intent,
                android.app.PendingIntent.FLAG_UPDATE_CURRENT or android.app.PendingIntent.FLAG_MUTABLE
            )

            session.commit(pendingIntent.intentSender)
            result.success(true)
        } catch (e: Exception) {
            session.abandon()
            result.error("INSTALL_FAILED", e.message, null)
        }
    }

    private fun showInstallDialog(apkFile: File, result: MethodChannel.Result) {
        val apkUri: Uri = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N) {
            FileProvider.getUriForFile(
                context,
                "${context.packageName}.provider",
                apkFile
            )
        } else {
            Uri.fromFile(apkFile)
        }

        val intent = Intent(Intent.ACTION_VIEW).apply {
            setDataAndType(apkUri, "application/vnd.android.package-archive")
            addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
            addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
        }
        context.startActivity(intent)
        result.success(true)
    }
}
