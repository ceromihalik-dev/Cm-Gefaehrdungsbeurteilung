# Build-, Test- und Releaseprozess – Version 0.1.3

## Anforderungen
- Windows 10/11 x64 für den Build
- .NET 10 **SDK** und Inno Setup 6/7 (nicht nur das .NET Runtimepaket)
- GitHub-Repository zur CI, falls genutzt; Actions bereitet ein **unsigniertes Test-Setup** vor
- Für automatische Updates: echtes Code-Signing-Zertifikat und Signiermöglichkeit (SignTool, Zeitstempelserver)
- Vor fachlichem Produktiveinsatz: geprüfter Sanierungskatalog und reale Anwender-/Geräte-/Drucktests

## A – Entwicklungs-/Testbuild

Auf Windows PowerShell im Projektordner:

```powershell
python .\tests\Validate-Source.py
.\scripts\Build-Setup.ps1
```

Danach im Ordner `release`:
`CM-Gefaehrdungsbeurteilung-Setup-0.1.3.exe`.

Dies ist **kein unterschriebenes Endnutzer-Release**. Ohne bei Erstellung fest eingebettete Zertifikatskennung ist die automatische Installation absichtlich deaktiviert. Updateprüfungen auf eine in der Oberfläche angegebene GitHub-Releasequelle sind unabhängig davon möglich.

## B – Automatischer Windows-Test

Nach Übernahme der Projektdateien in ein GitHub-Repository unter **Actions → Windows Build und Installer-Test (unsigniert) → Run workflow** starten. Der Workflow führt `dotnet publish`, Inno Setup und eine stille Neuinstallation, Wiederholungsinstallation sowie Deinstallation aus. Der Test erwartet, dass im Benutzerprofil angelegte Dateien bestehen bleiben. Das Ergebnis ist ein **unsigniertes Testartefakt**, keine öffentliche Produktivfreigabe.

Für eine manuelle Ausführung dieses destruktionssensiblen Installationstests ausschließlich eine frische Windows-VM verwenden:

```powershell
.\scripts\Test-Installer.ps1 -AllowLocalInstall
```

Das Skript verweigert den Test, wenn der erwartete Installations- oder Datenordner bereits vorhanden ist.

## C – Vorbereiteter signierter Releasepfad

1. Zertifikatsfingerabdruck des autorisierten Herausgebers verifizieren (40 Hex-Zeichen).
2. Reproduzierbaren Herausgeber-Build mit festem Fingerabdruck anfertigen:
   ` .\scripts\Build-Setup.ps1 -TrustedSignerThumbprint '<40-HEX-ZEICHEN>' `
3. Den erzeugten Installer mit einer **gültigen Authenticode-Signatur** versehen; empfohlene SHA-256-Dateidigest- und RFC-3161-Zeitstempeloptionen des Microsoft SignTools beachten.
4. Den signierten Installer mit dem Freigabeskript prüfen und `SHA256SUMS.txt` erzeugen:

   ```powershell
   .\scripts\Prepare-SignedRelease.ps1 -SignedInstallerPath '.\release\CM-Gefaehrdungsbeurteilung-Setup-0.1.3.exe' -ExpectedSignerThumbprint '<40-HEX-ZEICHEN>'
   ```

5. Signatur, Neu-/Upgradeinstallation, WPF-Oberfläche und Migrationsszenarien auf Windows freigeben.
6. Auf GitHub Release `v0.1.3` (kein Prerelease) die **beiden** Assets mit genau diesen Namen veröffentlichen:
   - `CM-Gefaehrdungsbeurteilung-Setup-0.1.3.exe`
   - `SHA256SUMS.txt`

**Wichtig:** Der von GitHub heruntergeladene Installer ist erst mit erfolgreicher SHA-256- und Authenticode-Prüfung zur Ausführung freigegeben. Ein von der Benutzeroberfläche frei eingetragenes Zertifikat ist kein Vertrauensanker. Die Zertifikatsrotation und eine automatische Datenbank-Rücksetzung sind nicht implementiert.

## D – Rückfallverfahren

Bei Installationsproblemen zuerst das Programm deinstallieren und den letzten geprüften Installer erneut installieren. Die Nutzerdaten bleiben unter `%LOCALAPPDATA%\CM\Gefaehrdungsbeurteilung`. Bei schemaändernden Programmversionen darf die alte Anwendung eine neue Datenbank nicht direkt weiterverwenden: Vorher mit Hilfe einer **passenden** Sicherung unter `Backups` wiederherstellen, stets erst auf einer Kopie testen. Für 0.1.3 bleibt das Schema auf 3, daher keine neue Migration.
