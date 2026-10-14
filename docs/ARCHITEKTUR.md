# Technisches Konzept – CM Gefährdungsbeurteilung 0.1.3

## Grundsätze

- Eigenständige Windows-Desktopanwendung; lokale Nutzung und Datenhoheit.
- UI mit C#/WPF unter .NET 10 LTS; SQLite über `Microsoft.Data.Sqlite` 10.0.12.
- Eine laufende Instanz pro Windows-Anmeldesitzung; gleichzeitige Bearbeitung wird in V0.1 nicht unterstützt.
- Programmdateien und Nutzerdaten sind getrennt.
- Inno Setup mit dauerhaft gleicher AppId für Neuinstallation und Updates.

## Bedienfluss

Übersicht → Beurteilungen → Stammdaten → Gefährdungskatalog mit Maßnahmen → Prüfung → später Dokumentation/PDF.

Die Fachkatalogeinträge sind nur Vorschläge und werden *pro Beurteilung kopiert*. Änderungen am Katalog können deshalb keine bereits dokumentierten, individuellen Maßnahmen unbemerkt verändern.

## Datenbank

`assessments` – Vorgang mit ID, Projekt, Adresse, Arbeitsbereich, Arbeitstext, Verantwortlichem, Status und Zeitstempeln.

`assessment_hazards` – mehrere Gefährdungen pro Vorgang, mit Vorlage-ID, Beschreibung, Maßnahmen, Verantwortlichen, Frist, Wirkungskontrolle und vorläufiger Einstufung.

`settings` – firmenbezogene Einstellungen, Updatequelle. Die Vertrauenskennung für Update-Installationen ist eine Assembly-Buildmetadaten-Konstante und wird nicht in der benutzeränderbaren SQLite-Datenbank gespeichert.

`PRAGMA user_version=3`. Zukünftige DB-Migrationen benötigen kontrollierte Migrationen `1 → 2 → 3 ...`, auf dem Original keine schreibende Migration ohne vorherige verifizierte Sicherung.

## Datensicherung und Datenschutz

SQLite-Dateien liegen nur im eigenen lokalen Windows-Benutzerprofil, nicht in der Programmablage. Tägliche automatische Backups (maximal 21 *automatische* Tagesstände) und zusätzliche, nicht automatisch gelöschte Backups vor Löschung/Update. SQLite Online-Backup-Funktion statt Datei-Kopie bei geöffneter Datenbank.

Eine Datenbankverschlüsselung ist **noch nicht implementiert**. Für vertrauliche Informationen geeignete Windows-Zugriffsschutz- und BitLocker-/Geräteverschlüsselung einsetzen. Spezielle DSGVO-Anforderungen einschließlich Lösch-/Aufbewahrungskonzept sind vor Produktivbetrieb zu konkretisieren.

## Updates

- GitHub Releases über HTTPS als Versionsquelle.
- Benutzerkonfigurierbares `owner/repository` und automatische Abfrage höchstens einmal täglich.
- Für die Updateinstallation müssen Release-Assets korrekt benannt sein: `CM-Gefaehrdungsbeurteilung-Setup-X.Y.Z.exe`, `SHA256SUMS.txt`.
- Erst SHA-256-Prüfung, danach Windows-Authenticode-Status `Valid` plus beim offiziellen Build unveränderlich eingebundener Signaturzertifikat-Fingerabdruck. Ein Entwicklungsbuild ohne diesen Wert kann lediglich auf Updates prüfen.
- Ohne gültige Signatur keine automatische Ausführung; manuelle Installation möglich.
- Vor dem Update wird ein DB-Backup erstellt und der bisherige Bearbeitungsstand gespeichert.
- Die Setup-Routine aktualisiert Programmdateien, Nutzerdaten werden nicht gelöscht.
- Für vollständige Produktsicherheit werden vor Freigabe Tests für Zertifikatswechsel, Rollback, Neuinstallation, fehlerhafte Downloads und Schema-Migrationen verlangt.

## Fachlichkeit und Freigaben

Die Risikowert-Matrix 1–3 × 1–3 ist ausdrücklich **eine vorläufige UI-/Entwicklungsannahme**, keine abschließend validierte Methode. Der Standardkatalog umfasst nur 15 bearbeitbare Beispiele, nicht alle denkbaren Gefährdungen. Eine echte Gefährdungsbeurteilung nach ArbSchG muss arbeitsplatz- und tätigkeitsbezogen unter Mitwirkung fachkundiger Personen durchgeführt werden. Insbesondere Tätigkeiten nach Gefahrstoff- und Biostoffrecht erfordern gesonderte Prüfungen und die jeweils einschlägigen Schutzmaßnahmen.

Eine im Programm dokumentierte Prüfung ist zunächst eine Statusmarkierung, kein Ersatz für eine rechtliche Unterzeichnung, Betriebsfreigabe, Messung oder erforderliche Qualifikation.

## Ergänzungen ab 0.1.1

`assessment_site_checks` ist eine eigene Tabelle für dokumentierte Antworten je Gefährdungsbeurteilung (Check-ID, Antwort, Befundtext, Kennzeichen für fachkundige Klärung). Ein neuer Prüfschritt wird für bestehende Projekte als ungeklärt ergänzt. Bestandsbeurteilungen mit vorherigem Status „Fachlich geprüft“ werden bei Migration auf „Erneute Prüfung erforderlich“ gesetzt; das ursprüngliche Datum bleibt erhalten. Die neuen Fragen müssen vor erneuter Prüfung beantwortet werden.

Schemaupgrade 1→2: vor schreibender Migration SQLite-Online-Backup, separat öffnen und `integrity_check` ausführen, dann Transaktion mit neuer Tabelle und `PRAGMA user_version=2`. Bei Fehler ohne Schemaänderung abbrechen.

Der Objekt-Check ist ein fachlicher Warn-/Dokumentationsmechanismus. Eine boolesche Bestätigung ist **kein Nachweis der Eignung**. Künftig sind Rollen, tatsächliche Freigabedokumente und eine geprüfte STOP-Logik erforderlich.


## Ergänzungen ab 0.1.3

`assessments.selected_activities` enthält eine kommaseparierte Liste **stabiler Tätigkeits-IDs** aus `ActivityService` (keine UI-Indices). Die Auswahl kann Fachbereiche kombinieren; der Objekt-Check umfasst alle betroffenen Bereiche. `ActivityService.Suggestions` gibt die zu prüfenden Katalogpositionen zurück. Vorschläge werden als frei bearbeitbare Kopien gespeichert, nicht als synchron mit dem Katalog verknüpfte Einträge.

Neue Datenbanken werden direkt mit Schema 3 angelegt. Für vorhandene Schemas 1/2 wird vor Schema 3 erneut per SQLite-Backup gesichert und auf Integrität geprüft, anschließend per Transaktion `selected_activities` ergänzt. Bereits geprüfte Alt-Beurteilungen erfordern erneute Prüfung. Kataloge und Vorschlagsbeziehungen sind weiterhin fachlich ungeprüfte Entwicklungsbeispiele.

Die Druckvorlage nutzt WPF FlowDocument, enthält Firmenlogo aus dem lokalen Datenordner und Maßnahmenübersicht. Direkter PDF- oder Word-Export ist nicht implementiert. Sicherung/Wiederherstellung von ergänzenden Dateien wie Firmenlogos ist für V0.3 vorgesehen und in der Testplanung nachzuziehen.

## Ergänzungen ab 0.1.3

Die einstellbare Vertrauenskennung (`repo.signerThumbprint`) wird nicht mehr verwendet. Ein offizieller Herausgeber-Build muss `TrustedUpdateSignerThumbprint` als Buildparameter festlegen. Dadurch wird der SHA-1-Zertifikatsfingerabdruck als `AssemblyMetadataAttribute` in den Build übernommen. Die Signaturprüfung bleibt zusätzlich verpflichtend und muss den Windows-Authenticode-Status `Valid` zurückliefern. Der Fingerabdruck ist **kein** Ersatz für die SHA-256-Dateiprüfsumme; er identifiziert den freigegebenen Zertifikatsinhaber.

Ohne gültige Buildmetadaten deaktiviert die Software den automatischen Installationsknopf. Das Update kann weiterhin gemeldet und die GitHub-Releaseseite geöffnet werden. Vor automatischen Updates werden aktuelle Beurteilungsdaten gespeichert und per SQLite-Online-Backup gesichert.

Der Update-Downloader begrenzt Setup-Downloads auf 500 MiB und nutzt einzigartige temporäre Dateinamen. **Eine Zertifikatsrotation ist noch nicht gelöst** und benötigt ein gesondertes, signiertes Vertrauensübergangskonzept.

Es gibt einen Windows-CI-Workflow, der `dotnet publish`, Inno Setup, wiederholte Silent-Installation und Deinstallation mit Prüfung der Datenverzeichnis-Trennung automatisiert. Die echte WPF-Oberfläche, der Druck und der vollständige signierte Updatepfad sind damit noch nicht abgenommen.
