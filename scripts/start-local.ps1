# Runs the platform locally without Docker.
# Requires: .NET 10 SDK, Node.js, SQL Server LocalDB (MSSQLLocalDB)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

Write-Host "Starting SQL Server LocalDB..." -ForegroundColor Cyan
sqllocaldb start MSSQLLocalDB | Out-Null

Write-Host "Starting API on http://localhost:5194 ..." -ForegroundColor Cyan
$api = Start-Process -FilePath "dotnet" `
    -ArgumentList @("run", "--project", "src/API/FlutterPlatform.API/FlutterPlatform.API.csproj", "--launch-profile", "http") `
    -WorkingDirectory (Join-Path $root "backend") `
    -PassThru

Start-Sleep -Seconds 3

Write-Host "Starting Web Dashboard on http://localhost:5173 ..." -ForegroundColor Cyan
$web = Start-Process -FilePath "npm" `
    -ArgumentList @("run", "dev") `
    -WorkingDirectory (Join-Path $root "web-dashboard") `
    -PassThru

Write-Host ""
Write-Host "API:        http://localhost:5194" -ForegroundColor Green
Write-Host "API docs:   http://localhost:5194/scalar/v1" -ForegroundColor Green
Write-Host "Dashboard:  http://localhost:5173" -ForegroundColor Green
Write-Host "Login:      admin@flutter-platform.local / Admin@123!" -ForegroundColor Yellow
Write-Host ""
Write-Host "Press Ctrl+C in this window to stop both processes." -ForegroundColor DarkGray

try {
    Wait-Process -Id $api.Id, $web.Id
}
finally {
    if (!$api.HasExited) { Stop-Process -Id $api.Id -Force -ErrorAction SilentlyContinue }
    if (!$web.HasExited) { Stop-Process -Id $web.Id -Force -ErrorAction SilentlyContinue }
}
