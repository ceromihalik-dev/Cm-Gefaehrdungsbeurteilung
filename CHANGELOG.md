# Änderungsverlauf – CM Gefährdungsbeurteilung

## 0.1.3 / Build 0001 – 14.10.2026
**Status: Quellcode erstellt, lokale statische und SQLite-Tests bestanden. Windows-Installer noch NICHT ausgeführt / freigegeben.**

- Einheitliche Version 0.1.3 in WPF, Assembly, Inno Setup, Buildskript, Vorschau und Dokumentation.
- Windows-Buildskript mit .NET-10-SDK-Prüfung, Inno-Setup-Erkennung, Kompilierungs-/Installer-Verifikation und verständlichen Fehlerhinweisen.
- CI vorbereitet: Windows-Runner baut die Anwendung und testet Silent-Installation, erneute Installation und Deinstallation mit Daten-Persistenzprüfung.
- Automatische Updates: Signaturzertifikatsfingerabdruck kommt ausschließlich aus vom Herausgeber gesetzten Assembly-Buildmetadaten; benutzereditierbares Feld entfernt. Entwicklungsbuilds installieren keine automatischen Updates.
- Download: maximale 500 MiB, zeitgleich laufende Downloads verwenden eindeutige temporäre Dateinamen; SHA-256- und Authenticode-Prüfung bleibt erforderlich.
- Signierter Releasepfad dokumentiert: `Prepare-SignedRelease.ps1` prüft Installer-Signatur und erzeugt erst danach `SHA256SUMS.txt`.
- SQLite-Schema unverändert 3, Datenverzeichnis getrennt von Installationsdateien.
- Behoben: fälschlich benutzerseitig als Vertrauensanker einstellbarer Signaturfingerabdruck.

## 0.1.2 / Build 0001 – 13.10.2026
- Mehrfachauswahl 13 Tätigkeiten; Katalogvorschläge mit Dublettenschutz.
- Datenbank-Schema 3, Backups und Migrationen für ältere Schema-Versionen.
- Druckvorlage mit Tätigkeitsliste, Maßnahmenübersicht, Logo und Unterschriftsfeld.
- GitHub-Windows-Buildworkflow angelegt.

## 0.1.1 / Build 0001 – 13.10.2026
- Vierstufiger Assistent mit Objekt-Check, Klärungspflichten und Prüfblockade bei ungeklärten Fällen.
- Datenbank-Schema 2, Backup vor Migration und Integritätsprüfung.

## 0.1.0 / Build 0001 – 13.10.2026
- WPF-Grundgerüst, Projekte, erste Gefährdungsbeispiele, SQLite, Windows-Druck und Updatevorbereitung.
