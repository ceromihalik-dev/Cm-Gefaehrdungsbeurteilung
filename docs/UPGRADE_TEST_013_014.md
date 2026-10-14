# Upgrade-Test 0.1.3 → 0.1.4

## Vorbedingungen
- Version 0.1.3 ist installiert und startet.
- Ausschließlich **Testdaten** verwenden.
- Neue 0.1.4-Setup.exe aus erfolgreichem GitHub-Actions-Artefakt herunterladen und entpacken.
- Die CI-Setup.exe ist **nicht signiert** und darf nicht als automatisches Update freigegeben werden.

## Testablauf
1. In 0.1.3 eine Testbeurteilung mit Projektadresse, mindestens zwei Tätigkeiten und mehreren Maßnahmen anlegen.
2. Beurteilung speichern, Anwendung schließen und wieder öffnen.
3. Die lokalen Nutzdaten unter `%LOCALAPPDATA%\CM\Gefaehrdungsbeurteilung` vollständig in einen separaten Sicherungsordner kopieren. Den Sicherungsordner nicht löschen.
4. 0.1.4-Setup.exe manuell starten; Installationsverzeichnis und AppId müssen unverändert bleiben.
5. Anwendung über das Startmenü öffnen und Version 0.1.4 prüfen.
6. Testbeurteilung öffnen und sämtliche Felder, Maßnahmen, Objekt-Check und Tätigkeiten vergleichen.
7. Erneut speichern, schließen und öffnen.
8. Druck/PDF erneut prüfen; Einstellungen → Updates öffnen.
9. Anwendung deinstallieren und prüfen, ob Nutzdaten erhalten bleiben. Bei Fehlern zuerst Backup sichern und **keine** produktiven Daten überschreiben.

## Abnahmekriterien
- Keine Fehlermeldungen bei Installation, Start und Datenzugriff.
- Testbeurteilung und Einstellungen vollständig erhalten.
- PDF-Ausgabe funktioniert weiterhin.
- Keine unbeabsichtigte automatische Installation eines unsignierten Updates.
- Testbericht mit PASS/FAIL und Screenshots dokumentieren.

## Sicherheitsgrenze
Das Bestehen dieses Tests bestätigt nur technische Upgrade-Eigenschaften, nicht die fachliche Eignung der Gefährdungsbeurteilung.
