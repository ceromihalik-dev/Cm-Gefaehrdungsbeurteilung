# Entwicklung auf GitHub

## Aktueller Stand
Windows-Installation 0.1.3 / Build 0001: 7/7 Anwenderprüfungen bestanden.
Die lokale Quellcodekorrektur des CS0201-Fehlers muss im GitHub-Stand enthalten sein.

## Erstübernahme vom Windows-PC
1. Repository klonen: `git clone https://github.com/ceromihalik-dev/Cm-Gefaehrdungsbeurteilung.git`
2. Aus `D:\cm-beurteilung` die Verzeichnisse `src`, `installer`, `scripts` und fachliche Dokumentation in den Klon kopieren.
3. Keine lokalen Datenbanken, Zertifikate, Kennwörter, `bin`, `obj`, `publish` oder `release` übernehmen.
4. `git status` prüfen, danach `git add .`, `git commit -m "feat: import tested Windows baseline 0.1.3"` und `git push`.
5. GitHub Actions > Windows Setup (test) ausführen.

## Release-Sicherheit
CI erstellt ausschließlich **unsignierte Testinstallationspakete**. Automatische Updateinstallationen erst nach Herausgebersignaturprüfung, SHA-256-Verifikation und Upgrade-/Rollback-Tests freigeben.
Der erfolgreiche Windows-Build allein ist keine fachliche Freigabe der Gefährdungsbeurteilungen.
