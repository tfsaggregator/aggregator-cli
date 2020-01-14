@echo off

REM Search if .Net Core installed
where /Q dotnet
IF %ERRORLEVEL% NEQ 0 (
    echo .Net Core Runtime is missing, please install from https://dotnet.microsoft.com/download
    goto bye
)

REM Check .Net Core version
FOR /f "delims=-" %%i IN ('dotnet --version') DO SET _DOTNETVER=%%i
IF q%_DOTNETVER:2.1=%==q%_DOTNETVER% (
    echo Another version of .Net Core Runtime is installed, please install version 2.1 from https://dotnet.microsoft.com/download
    goto bye
)

REM All good run aggregator forwarding any parameter
dotnet %~dp0aggregator-cli.dll %*

:bye