# Roadmap – CM Gefährdungsbeurteilung

## 0.1.3 – QUELLCODE – Windows-Build und Installer-Test vorbereitet
- [x] Versionen in WPF, Projektdatei, Inno Setup, HTML-Vorschau und Skripten vereinheitlicht
- [x] Herausgeberidentität für automatische Updates im Programm-Build statt in SQLite/Benutzeroberfläche
- [x] Größenbegrenzung für Setup-Downloads und eindeutige Download-Temporärdateien
- [x] Windows-Skripte: Build, Silent-Installation, In-Place-Update, Deinstallation und Datenpersistenzprüfung
- [x] Signatur-Prüfskript zur kontrollierten Erstellung von `SHA256SUMS.txt`
- [x] Automatische Quellcode- und SQLite-Regressionstests lokal bestanden
- [ ] GitHub Actions ausführen und WPF-Kompilierung bestätigen
- [ ] Installer und Update mit echter früherer Version 0.1.2 auf Windows testen
- [ ] Erstes signiertes Windows-Release und vollständiger Update-End-to-End-Test

## 0.1.2 – historischer Quellcodestand
- [x] Mehrfachauswahl 13 Sanierungstätigkeiten und Vorschläge für Gefährdungen
- [x] Zusammenführen ohne doppelte Katalogpositionen, aber mit erforderlicher fachlicher Prüfung
- [x] SQLite Schema 3 und geprüfte Migrationen 0/1/2 → 3
- [x] Erweiterter Druckbericht: Firmenlogo, Tätigkeiten, Maßnahmenübersicht, Unterschrift
- [x] Interaktive Vorschau und automatisierte Quellcode-/SQL-Tests
- [ ] Echter Windows-Build + Inno-Setup-Installation mit Abnahmetests
- [ ] Update von 0.1.1 auf 0.1.2 auf einem Windows-Testsystem
- [ ] Benutzerführung und UI-Funktionen gegen reale WPF-Anwendung prüfen

## 0.1.1 – historischer Quellcodestand
- [x] 4-stufiger Beurteilungsablauf mit objektspezifischem Sicherheitscheck
- [x] Prüfung blockiert bei ungeklärtem Objekt-Check / nicht dokumentierter Klärung
- [x] SQLite-Migration Schema 1 → 2 mit vorangehender Sicherungsprüfung
- [x] Berichts-/Druckausgabe um Standortcheck ergänzt
- [x] Update-Setup-Name an Release-Version gebunden
- [ ] Tatsächlicher Windows-Build und installierbare Setup.exe
- [ ] Windows-Test: Neuerfassung, Editieren, Schema-1-Migration, Ausdruck, Sicherungen, Deinstallation
- [ ] Überprüfung UI/Bedienbarkeit/Barrierefreiheit auf Windows

## 0.2.0 – FACHLICHER KATALOG
- [ ] Vollständige, fachkundig geprüfte Vorlagen mit Quellen, Version und Regeln für Gefahrstoffe/Biostoffe
- [ ] Objekt-Check mit expliziten Fachfreigaben und Nachweisanhängen
- [ ] Tätigkeitsassistent, automatische Vorschlagsauswahl und STOP-Hierarchie
- [ ] Fachlich validierte Bewertungsmethode statt vorläufiger 1–3-Matrix
- [ ] Direkter PDF-/Word-Export und Corporate Design

## 0.3.0 – DOKUMENTATION
- [ ] Maßnahmenstatus, Verantwortlichkeiten, Termine, Wirksamkeitskontrolle
- [ ] Anlagen/Fotos, Unterweisung, Dokumentrevisionen und Historie
- [ ] Wiederherstellung, Versionierung und Tests für weitere Schemamigrationen

## 1.0.0 – PRODUKTIVRELEASE
- [ ] Signierte Installer/Releases, CI-Signierung und abgesicherter Zertifikatswechsel
- [ ] Komplett getestetes Updatesystem, Wiederherstellung und Rollback
- [ ] Fachlich freigegebener Katalog; Rechtsquellen und Versionspflege
- [ ] Datenschutz-/Sicherheitskonzept und Abnahmetests
