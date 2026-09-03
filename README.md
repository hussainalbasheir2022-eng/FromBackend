# Flutter Platform — Private Cloud Flutter IDE + OTA Deployment

A production-grade platform for developing Flutter Android applications from a browser-based IDE and automatically deploying them to managed Android devices via OTA updates.

## Architecture

```
Browser (VS Code-like IDE)
    ↓ HTTPS
ASP.NET Core API (.NET 10 + SQL Server)
    ↓ Redis Queue
Build Worker (Docker + Flutter SDK + Android SDK)
    ↓ MinIO (S3)
Artifact Storage
    ↓ SignalR
Android Devices (Auto-update via PackageInstaller)
```

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core Web API, .NET 10, Clean Architecture, CQRS, MediatR |
| Database | SQL Server 2022 |
| Cache / Queue | Redis |
| Storage | MinIO (S3-compatible) |
| Real-time | SignalR |
| Build Worker | .NET Worker Service + Docker + Flutter 3.27 + Android SDK 34 |
| Web Dashboard | React + TypeScript + Vite + Monaco Editor + Tailwind CSS |
| Android Client | Flutter + PackageInstaller API + SignalR |

## Quick Start (Docker Compose)

```bash
cd docker
docker compose up -d
```

Services:
- **API**: http://localhost:8080
- **API Docs (Scalar)**: http://localhost:8080/scalar/v1
- **Web Dashboard**: http://localhost:3000
- **MinIO Console**: http://localhost:9001
- **SQL Server**: localhost:1433

Default admin credentials:
- Email: `admin@flutter-platform.local`
- Password: `Admin@123!`

## Development

### Backend

```bash
cd backend
dotnet run --project src/API/FlutterPlatform.API
```

Requires SQL Server and Redis running locally (use `docker compose up sqlserver redis minio -d`).

### Web Dashboard

```bash
cd web-dashboard
npm install
npm run dev
```

### Build Worker

The worker runs on Linux with Flutter and Android SDK installed.
Use Docker for local testing:

```bash
cd build-worker
docker build -t flutter-build-worker .
docker run --env-file .env flutter-build-worker
```

## Project Workflow

1. **Create Project** → Dashboard → New Project
2. **Open IDE** → Click "Open IDE"
3. **Write Dart Code** → Monaco Editor with syntax highlighting
4. **Save** (Ctrl+S) → Files saved to server
5. **Build** → Flutter SDK compiles APK on Build Worker
6. **Publish** → Create release, sign APK, notify devices via SignalR
7. **Device Update** → Android device downloads, verifies SHA-256, installs silently

## Android Device Setup

### Managed Device (Device Owner mode)

1. Factory reset the device
2. During setup wizard, scan QR code or use ADB to provision as Device Owner:

```bash
adb shell dpm set-device-owner com.flutterplatform.app/.AdminReceiver
```

3. Install the bootstrap APK (signed with your release key)
4. The app registers itself and receives updates automatically

### APK Signing

Store your release keystore securely. Never commit it to source control.

```bash
# Generate keystore (one time)
keytool -genkey -v -keystore release.keystore \
  -alias release -keyalg RSA -keysize 2048 -validity 10000

# Copy to Docker secrets volume
cp release.keystore /path/to/signing_secrets/
```

Configure in `build-worker/src/FlutterPlatform.BuildWorker/appsettings.json`:

```json
{
  "Signing": {
    "KeystorePath": "/secrets/release.keystore",
    "KeystorePassword": "YOUR_PASSWORD",
    "KeyAlias": "release",
    "KeyPassword": "YOUR_KEY_PASSWORD"
  }
}
```

⚠️ **Critical**: All APK updates must use the same signing key as the initially installed APK. Changing the key will cause Android to reject the update.

## Scaling Build Workers

```bash
docker compose up --scale worker=4
```

All workers consume from the same Redis queue.

## API Documentation

Available at http://localhost:8080/scalar/v1 when running.

Key endpoints:

```
POST /api/v1/auth/login
POST /api/v1/auth/register

GET  /api/v1/projects
POST /api/v1/projects
GET  /api/v1/projects/{id}/files
PUT  /api/v1/projects/{id}/files/{path}
POST /api/v1/projects/{id}/build

GET  /api/v1/builds
GET  /api/v1/builds/{id}/logs

POST /api/v1/releases
POST /api/v1/releases/{id}/publish
POST /api/v1/releases/{id}/rollback

GET  /api/v1/devices
POST /api/v1/devices/register
POST /api/v1/devices/heartbeat
GET  /api/v1/updates/latest
```

## SignalR Events

| Event | Hub | Description |
|---|---|---|
| `build.started` | `/hubs/build` | Build job started |
| `build.log` | `/hubs/build` | Real-time log line |
| `build.completed` | `/hubs/build` | Build finished |
| `deployment.available` | `/hubs/deployment` | New release published |
| `device.online` | `/hubs/deployment` | Device came online |
| `device.versionChanged` | `/hubs/deployment` | Device updated version |
