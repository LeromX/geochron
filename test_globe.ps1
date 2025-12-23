# Test script to run globe screensaver with debug logging
$debugLogPath = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), "geochron_debug.log")

# Clear any existing debug log
if (Test-Path $debugLogPath) {
    Remove-Item $debugLogPath
}

Write-Host "Starting screensaver test..."
Write-Host "Debug log will be written to: $debugLogPath"
Write-Host ""

# Run the screensaver (will run until user closes it)
$exePath = "C:\1. Code\Geochron\src\GeochronScreensaver\bin\Debug\net8.0-windows\GeochronScreensaver.exe"
Start-Process -FilePath $exePath -ArgumentList "/s"

# Wait a moment for the screensaver to initialize
Start-Sleep -Seconds 2

# Display the debug log
Write-Host "=== Debug Log Contents ==="
if (Test-Path $debugLogPath) {
    Get-Content $debugLogPath
} else {
    Write-Host "ERROR: Debug log file not created!"
}

Write-Host ""
Write-Host "Press any key to view updated log (or Ctrl+C to exit)..."
Read-Host

# Display updated log
Write-Host "=== Updated Debug Log ==="
if (Test-Path $debugLogPath) {
    Get-Content $debugLogPath
} else {
    Write-Host "ERROR: Debug log file not created!"
}
