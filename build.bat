@echo off
echo ========================================
echo   Geochron Screensaver Build Script
echo ========================================
echo.

if "%1"=="" goto debug
if "%1"=="debug" goto debug
if "%1"=="release" goto release
if "%1"=="portable" goto portable
if "%1"=="run" goto run
goto help

:debug
echo Building DEBUG version...
dotnet build src\GeochronScreensaver\GeochronScreensaver.csproj -c Debug
if %ERRORLEVEL%==0 (
    echo.
    echo SUCCESS! To run: dotnet run --project src\GeochronScreensaver
)
goto end

:release
echo Building RELEASE version (.scr file)...
dotnet build src\GeochronScreensaver\GeochronScreensaver.csproj -c Release
if %ERRORLEVEL%==0 (
    echo.
    echo SUCCESS! Screensaver at:
    echo   src\GeochronScreensaver\bin\Release\net8.0-windows\GeochronScreensaver.scr
    echo.
    echo Right-click the .scr file and select "Install"
)
goto end

:portable
echo Building PORTABLE version (self-contained)...
if not exist build\portable mkdir build\portable
dotnet publish src\GeochronScreensaver\GeochronScreensaver.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o build\portable
if %ERRORLEVEL%==0 (
    if exist build\portable\GeochronScreensaver.exe (
        if exist build\portable\GeochronScreensaver.scr del build\portable\GeochronScreensaver.scr
        ren build\portable\GeochronScreensaver.exe GeochronScreensaver.scr
    )
    echo.
    echo SUCCESS! Portable screensaver at:
    echo   build\portable\GeochronScreensaver.scr
    echo.
    echo This runs on any Windows x64 PC without .NET installed!
)
goto end

:run
echo Running screensaver in test mode...
dotnet run --project src\GeochronScreensaver\GeochronScreensaver.csproj
goto end

:help
echo Usage: build.bat [mode]
echo.
echo Modes:
echo   debug     - Build debug version (default)
echo   release   - Build release .scr file
echo   portable  - Build self-contained .scr (no .NET needed)
echo   run       - Build and run for testing
echo.
goto end

:end
echo.
