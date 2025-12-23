# Geochron Screensaver Build Script
# Run: powershell -ExecutionPolicy Bypass -File build.ps1

param(
    [ValidateSet("debug", "release", "portable", "install")]
    [string]$Mode = "debug"
)

$ProjectPath = "src\GeochronScreensaver\GeochronScreensaver.csproj"
$OutputDir = "build"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Geochron Screensaver Build Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

switch ($Mode) {
    "debug" {
        Write-Host "Building DEBUG version..." -ForegroundColor Yellow
        dotnet build $ProjectPath -c Debug

        if ($LASTEXITCODE -eq 0) {
            Write-Host ""
            Write-Host "SUCCESS! To run:" -ForegroundColor Green
            Write-Host "  dotnet run --project $ProjectPath" -ForegroundColor White
            Write-Host ""
            Write-Host "Or with screensaver mode:" -ForegroundColor Gray
            Write-Host "  dotnet run --project $ProjectPath -- /s" -ForegroundColor White
        }
    }

    "release" {
        Write-Host "Building RELEASE version (.scr file)..." -ForegroundColor Yellow
        dotnet build $ProjectPath -c Release

        if ($LASTEXITCODE -eq 0) {
            $scrPath = "src\GeochronScreensaver\bin\Release\net8.0-windows\GeochronScreensaver.scr"
            Write-Host ""
            Write-Host "SUCCESS! Screensaver built at:" -ForegroundColor Green
            Write-Host "  $scrPath" -ForegroundColor White
            Write-Host ""
            Write-Host "To install:" -ForegroundColor Gray
            Write-Host "  1. Right-click the .scr file -> Install" -ForegroundColor White
            Write-Host "  2. Or copy to C:\Windows\System32\" -ForegroundColor White
        }
    }

    "portable" {
        Write-Host "Building PORTABLE version (self-contained, no .NET required)..." -ForegroundColor Yellow

        # Create output directory
        if (!(Test-Path $OutputDir)) {
            New-Item -ItemType Directory -Path $OutputDir | Out-Null
        }

        # Publish self-contained single file
        dotnet publish $ProjectPath `
            -c Release `
            -r win-x64 `
            --self-contained true `
            -p:PublishSingleFile=true `
            -p:IncludeNativeLibrariesForSelfExtract=true `
            -p:EnableCompressionInSingleFile=true `
            -o "$OutputDir\portable"

        if ($LASTEXITCODE -eq 0) {
            # Rename to .scr
            $exePath = "$OutputDir\portable\GeochronScreensaver.exe"
            $scrPath = "$OutputDir\portable\GeochronScreensaver.scr"

            if (Test-Path $exePath) {
                if (Test-Path $scrPath) { Remove-Item $scrPath }
                Rename-Item $exePath $scrPath
            }

            Write-Host ""
            Write-Host "SUCCESS! Portable screensaver built at:" -ForegroundColor Green
            Write-Host "  $scrPath" -ForegroundColor White
            Write-Host ""
            Write-Host "This file can run on any Windows 10/11 x64 PC" -ForegroundColor Gray
            Write-Host "without installing .NET!" -ForegroundColor Gray
        }
    }

    "install" {
        Write-Host "Building and installing screensaver..." -ForegroundColor Yellow
        dotnet build $ProjectPath -c Release

        if ($LASTEXITCODE -eq 0) {
            $scrPath = "src\GeochronScreensaver\bin\Release\net8.0-windows\GeochronScreensaver.scr"
            $destPath = "$env:WINDIR\System32\GeochronScreensaver.scr"

            Write-Host ""
            Write-Host "Copying to System32 (requires admin)..." -ForegroundColor Yellow

            try {
                Copy-Item $scrPath $destPath -Force -ErrorAction Stop
                Write-Host ""
                Write-Host "SUCCESS! Screensaver installed." -ForegroundColor Green
                Write-Host "Open Settings -> Personalization -> Lock screen -> Screen saver" -ForegroundColor White
            }
            catch {
                Write-Host ""
                Write-Host "ERROR: Admin rights required. Run PowerShell as Administrator." -ForegroundColor Red
                Write-Host ""
                Write-Host "Or manually copy:" -ForegroundColor Gray
                Write-Host "  From: $scrPath" -ForegroundColor White
                Write-Host "  To:   $destPath" -ForegroundColor White
            }
        }
    }
}

Write-Host ""
