@echo off
setlocal
echo [Build] Building AutoNet.exe ...
set "CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if not exist "%CSC%" (
    echo [ERROR] csc.exe not found at %CSC%
    exit /b 1
)

"%CSC%" /nologo /target:winexe /platform:anycpu /optimize+ /win32icon:app.ico /out:AutoNet.exe src\*.cs

if %ERRORLEVEL% EQU 0 (
    echo [SUCCESS] AutoNet.exe build succeeded!
) else (
    echo [FAILED] Compilation failed with error code %ERRORLEVEL%
    exit /b 1
)
endlocal
