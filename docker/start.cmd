@ECHO OFF

REM Bypass "Terminate Batch Job" prompt.
if "%~1"=="-FIXED_CTRL_C" (
   REM Remove the -FIXED_CTRL_C parameter
   SHIFT
) ELSE (
   REM Run the batch with <NUL and -FIXED_CTRL_C
   CALL <NUL %0 -FIXED_CTRL_C %*
   GOTO :EOF
)

IF EXIST "%Aggregator_AzureDevOpsCertificate%" (
    ECHO Importing %Aggregator_AzureDevOpsCertificate%
    certoc -addstore root "%Aggregator_AzureDevOpsCertificate%"
    ECHO Import completed
    ECHO.
)
dotnet aggregator-host.dll

EXIT
