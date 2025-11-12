@echo off
REM Check if service is running
sc query DeviceHubMiniService | findstr /I "RUNNING" >nul
if %errorlevel%==0 (
    echo Service is running. Stopping DeviceHubMiniService...
    net stop DeviceHubMiniService
) else (
    echo Service is not running.
)

REM Prevent Pre-Build Event from failing
exit /b 0
