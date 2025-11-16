@echo off
echo Setting up New Relic environment variables...

REM Replace YOUR_LICENSE_KEY_HERE with your actual New Relic license key
set NEW_RELIC_LICENSE_KEY=8241dbd393ea5ad28da6c8924edc15cbFFFFNRAL
set NEW_RELIC_APP_NAME=DeviceHubMini
set CORECLR_ENABLE_PROFILING=1
set CORECLR_PROFILER={36032161-FFC0-4B61-B559-F6C5D41BAE5A}
set CORECLR_NEWRELIC_HOME=C:\ProgramData\New Relic\.NET Agent\
set CORECLR_PROFILER_PATH=C:\ProgramData\New Relic\.NET Agent\newrelic.profiler.dll

echo New Relic environment variables set.
echo License key configured successfully.
pause