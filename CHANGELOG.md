# Änderungsverlauf – CM Gefährdungsbeurteilung

## 0.1.9 / Build 0001 – 14.10.2026
**Status: GitHub-Quellcode und CI-Prüfungen vorbereitet; Windows-Build und Installation noch nicht bestätigt.**

- Einheitliche Versionsprüfung für Anwendung, Assembly, Dateiversion und Inno Setup vor dem Build eingeführt.
- GitHub Actions führt die Versionsprüfung vor dem Setup-Build aus und bricht bei Abweichungen ab.
- Buildskript ermittelt die Version künftig aus der Projektdatei; feste Versionsprüfung pro Release entfällt.
- Bestehende SHA-256-Prüfung, Authenticode-Herausgeberprüfung und Trennung zwischen Test-Artifact und signiertem Release bleiben bestehen.
- Datenbankschema unverändert 3.

## 0.1.8 / Build 0001 – 14.10.2026
**Status: Quellcode aktualisiert; Windows-Build und Installation noch nicht getestet.**

- Automatische Updateprüfung speichert den letzten Prüfzeitpunkt erst nach erfolgreicher Antwort der GitHub-Releases-API.
- Manuelle Updateprüfung setzt zuvor gespeicherte Release-Informationen, Freigabeschaltflächen und Anmerkungen zurück; alte Ergebnisse bleiben nicht sichtbar.
- Erfolgreiche manuelle Prüfungen aktualisieren ebenfalls den Zeitpunkt der letzten Prüfung.
- Signatur- und SHA-256-Anforderungen für automatische Installationen bleiben unverändert.
- Datenbankschema unverändert 3.

## 0.1.7 / Build 0001 – 14.10.2026
**Status: GitHub-Quellcode aktualisiert; Windows-Build noch nicht bestätigt.**

- Im Updatebereich direkter Zugang zu GitHub Actions / Windows-Testversionen ergänzt.
- Klare Trennung zwischen unsignierten CI-Artifacts (manuell) und signierten GitHub Releases (für automatische Installation).
- Bei fehlendem öffentlichen Release wird eine verständliche Anleitung statt einer generischen Fehlermeldung angezeigt.
- Bereits vorhandene Verifikation von SHA-256 und Herausgebersignatur bleibt erhalten; Sicherheitsprüfung wurde nicht abgeschwächt.
- Datenbankschema unverändert 3; Nutzerdaten werden beim Setup getrennt vorgehalten.

## 0.1.6 / Build 0001 – 14.10.2026
**Status: Release-Vorbereitung, ohne signierte Freigabe und ohne bestätigten Windows-Test.**

- Signiertes Release-Vorbereitungsskript prüft Installername anhand der aktuellen Projektversion statt der alten festen Version 0.1.3.
- Prüfskript verlangt gültige Authenticode-Herausgebersignatur und erzeugt SHA256SUMS.txt erst nach erfolgreicher Kontrolle.
- GitHub Actions erzeugt weiterhin nur unsignierte manuelle Test-Artefakte. Automatische In-App-Installation wird erst für signierte, veröffentlichte Releases aktiviert.
- Datenbankschema unverändert 3.

## 0.1.5 / Build 0001 – 14.10.2026
**Status: Quellcode und Katalog-Fachprüfung erstellt; Windows-Build und fachkundige Freigabe noch ausstehend.**

- Katalog um fünf Einträge auf insgesamt 21 ergänzt (verdeckte Installationen, Trocknungsgeräte, belastete Trocknungsluft und Filter, Löschwasser, Brandrückstandsstaub).
- Neue Einträge passenden Sanierungstätigkeiten zugeordnet.
- Fachliche Einzelprüfung aller 21 Katalogpositionen mit Risikoschwerpunkten, Quellen und erforderlichen Ergänzungen dokumentiert: docs/FACHPRUEFUNG_KATALOG_2026-10-14.md.
- Datenbankschema unverändert 3. Neue Katalogpositionen werden als Vorschläge bereitgestellt; vorhandene Beurteilungen werden nicht automatisch verändert.
- Katalogvorschläge sind ausdrücklich keine fachkundige Freigabe.

## 0.1.4 / Build 0001 – 14.10.2026
**Status: Entwicklungsstand; GitHub-CI und Upgradeprüfung noch ausstehend.**

- Versionsnummer für Windows-Anwendung, Setup und Buildskript auf 0.1.4 angehoben.
- Datenbankschema 3 bleibt unverändert; Update-Test von 0.1.3 mit Datenerhalt vorgesehen.
- Automatische Installation weiterhin ausschließlich für korrekt signierte und SHA-256-geprüfte Releases. GitHub-CI-Artefakte sind unsignierte Testpakete.

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
