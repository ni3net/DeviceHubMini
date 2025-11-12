@echo off
setlocal

:: =====================================
:: DeviceHubMini Windows Service Installer
:: =====================================

set SERVICE_NAME=DeviceHubMiniService
set SERVICE_EXE=%~dp0DeviceHubMini.Service.exe

:menu
cls
echo =====================================
echo   Windows Service Manager - %SERVICE_NAME%
echo =====================================
echo.
echo   1. Install Service
echo   2. Start Service
echo   3. Stop Service
echo   4. Restart Service
echo   5. Delete Service
echo   6. Check Status
echo   0. Exit
echo.
set /p choice=Enter your choice: 

if "%choice%"=="1" goto install
if "%choice%"=="2" goto start
if "%choice%"=="3" goto stop
if "%choice%"=="4" goto restart
if "%choice%"=="5" goto delete
if "%choice%"=="6" goto status
if "%choice%"=="0" goto end
goto menu

:install
echo.
echo =====================================
echo   INSTALLING %SERVICE_NAME%
echo =====================================
echo.

:: Prompt user for API key securely
set /p API_KEY=Enter GraphQL API Key (will be encrypted and stored securely): 

if "%API_KEY%"=="" (
    echo [ERROR] API key cannot be empty.
    pause
    goto menu
)

:: Create service with API key argument
echo Creating service with API key parameter...
sc create %SERVICE_NAME% binPath= "\"%SERVICE_EXE%\" --apikey %API_KEY%" start= auto

if %errorlevel% neq 0 (
    echo [ERROR] Failed to create service.
    pause
    goto menu
)

echo Service %SERVICE_NAME% installed successfully.
echo Starting service for the first time to encrypt API key...
net start %SERVICE_NAME%

echo.
echo Once the key is encrypted, the service will no longer need the key as a startup argument.
echo You can safely remove the API key from memory.
pause
goto menu

:start
echo Starting service %SERVICE_NAME%...
net start %SERVICE_NAME%
pause
goto menu

:stop
echo Stopping service %SERVICE_NAME%...
net stop %SERVICE_NAME%
pause
goto menu

:restart
echo Restarting service %SERVICE_NAME%...
net stop %SERVICE_NAME%
timeout /t 2 >nul
net start %SERVICE_NAME%
pause
goto menu

:delete
echo Deleting service %SERVICE_NAME%...
sc delete %SERVICE_NAME%
pause
goto menu

:status
sc query %SERVICE_NAME%
pause
goto menu

:end
echo Exiting...
endlocal
exit
