@echo off
setlocal EnableExtensions DisableDelayedExpansion
title CM Gefaehrdungsbeurteilung - Setup Builder 0.1.3

rem ============================================================
rem CM Gefaehrdungsbeurteilung - Windows Setup erstellen
rem Arbeitsverzeichnis: D:\cm-beurteilung
rem Benutzt die Build-Setup.ps1 aus Projektversion 0.1.3.
rem Falls Inno Setup fehlt: Installation nur nach Bestaetigung.
rem ============================================================
set "BASE=D:\cm-beurteilung"
set "ROOT=%BASE%"
set "ISCC="

echo.
echo ============================================================
echo   CM Gefaehrdungsbeurteilung - Windows Setup erstellen
echo ============================================================
echo.

if exist "%ROOT%\scripts\Build-Setup.ps1" goto project_found
set "ROOT=%BASE%\CM-Gefaehrdungsbeurteilung-0.1.3"
if exist "%ROOT%\scripts\Build-Setup.ps1" goto project_found

echo [FEHLER] scripts\Build-Setup.ps1 wurde nicht gefunden.
echo Erwartete Pfade:
echo  %BASE%\scripts\Build-Setup.ps1
echo  %BASE%\CM-Gefaehrdungsbeurteilung-0.1.3\scripts\Build-Setup.ps1
goto failed

:project_found
echo [INFO] Projekt: %ROOT%
if not exist "%ROOT%\src\CM.Gefaehrdungsbeurteilung\CM.Gefaehrdungsbeurteilung.csproj" (
  echo [FEHLER] Die Projektdatei ^(.csproj^) fehlt.
  goto failed
)
where powershell.exe >nul 2>&1
if errorlevel 1 (
  echo [FEHLER] Windows PowerShell wurde nicht gefunden.
  goto failed
)
where dotnet.exe >nul 2>&1
if errorlevel 1 (
  echo [FEHLER] .NET SDK wurde nicht gefunden.
  echo Download: https://dotnet.microsoft.com/download/dotnet/10.0
  goto failed
)

echo [1/3] Suche Inno Setup Compiler ISCC.exe ...
:search_iscc
set "ISCC="
if defined CM_ISCC if exist "%CM_ISCC%" set "ISCC=%CM_ISCC%"
for %%P in (
  "%ProgramFiles%\Inno Setup 7\ISCC.exe"
  "%ProgramFiles%\Inno Setup 6\ISCC.exe"
  "%ProgramFiles(x86)%\Inno Setup 7\ISCC.exe"
  "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
  "%LOCALAPPDATA%\Programs\Inno Setup 7\ISCC.exe"
  "%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe"
  "%LOCALAPPDATA%\Inno Setup 7\ISCC.exe"
  "%LOCALAPPDATA%\Inno Setup 6\ISCC.exe"
) do (
  if not defined ISCC if exist "%%~P" set "ISCC=%%~P"
)
if defined ISCC goto compiler_found
for /f "delims=" %%P in ('where ISCC.exe 2^>nul') do (
  if not defined ISCC set "ISCC=%%~fP"
)
if defined ISCC goto compiler_found

echo.
echo [HINWEIS] Inno Setup 6/7 konnte nicht gefunden werden.
echo Offizieller Download: https://jrsoftware.org/isdl.php
echo.
where winget.exe >nul 2>&1
if errorlevel 1 goto manual_path
choice /C JNA /N /M "Inno Setup 7 jetzt installieren? [J]a [N]ein [A]nderer Pfad: "
if errorlevel 3 goto manual_path
if errorlevel 2 goto failed
if errorlevel 1 goto install_inno

:install_inno
echo.
echo [2/3] Installiere Inno Setup 7 ueber winget ...
echo Hinweis: Der offizielle Installationsassistent wird geoeffnet.
winget install --id JRSoftware.InnoSetup.7 -e -s winget -i
if errorlevel 1 (
  echo [WARNUNG] winget konnte Inno Setup nicht installieren.
  goto manual_path
)
echo.
echo [INFO] Suche Compiler nach Installation erneut ...
goto search_after_install

:search_after_install
rem Nach einer Installation erneut nach den Standardpfaden suchen.
set "ISCC="
for %%P in (
  "%ProgramFiles%\Inno Setup 7\ISCC.exe"
  "%ProgramFiles(x86)%\Inno Setup 7\ISCC.exe"
  "%LOCALAPPDATA%\Programs\Inno Setup 7\ISCC.exe"
  "%LOCALAPPDATA%\Inno Setup 7\ISCC.exe"
) do (
  if not defined ISCC if exist "%%~P" set "ISCC=%%~P"
)
if defined ISCC goto compiler_found
echo [HINWEIS] Installation abgeschlossen, Compilerpfad unbekannt.

:manual_path
echo.
echo Bitte den vollstaendigen Pfad zu ISCC.exe angeben.
echo Beispiel: C:\Program Files\Inno Setup 7\ISCC.exe
echo Falls noch nicht installiert, zuerst Inno Setup installieren.
set "CUSTOM="
set /p "CUSTOM=Pfad zu ISCC.exe (Enter = Abbruch): "
if not defined CUSTOM goto failed
set "CUSTOM=%CUSTOM:"=%"
if not exist "%CUSTOM%" (
  echo [FEHLER] Datei nicht gefunden: %CUSTOM%
  goto failed
)
for %%P in ("%CUSTOM%") do if /I not "%%~nxP"=="ISCC.exe" (
  echo [FEHLER] Gewuenscht ist die Datei ISCC.exe.
  goto failed
)
set "ISCC=%CUSTOM%"

:compiler_found
echo [OK] Inno Setup: %ISCC%
echo.
echo [3/3] Baue Windows-Anwendung und Setup ...
cd /d "%ROOT%"
if errorlevel 1 goto failed
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%ROOT%\scripts\Build-Setup.ps1" -ISCC "%ISCC%"
if errorlevel 1 goto build_failed

echo.
echo ============================================================
echo [ERFOLG] Windows-Setup wurde erstellt.
echo Ausgabeordner: %ROOT%\release
echo ============================================================
echo.
echo Dies ist vorerst ein UNSIGNIERTES TEST-SETUP.
echo Bitte Installation und Aktualisierung auf Windows testen.
echo.
start "" "%ROOT%\release"
pause
exit /b 0

:build_failed
echo.
echo [FEHLER] Der Setup-Build wurde nicht abgeschlossen.
echo Bitte die genaue Fehlermeldung oberhalb pruefen.
:failed
echo.
echo [HINWEIS] Inno Setup Download: https://jrsoftware.org/isdl.php
echo.
pause
exit /b 1
