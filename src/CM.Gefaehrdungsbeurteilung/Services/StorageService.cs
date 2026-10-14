using System.Globalization;
using System.IO;
using CM.Gefaehrdungsbeurteilung.Models;
using Microsoft.Data.Sqlite;

namespace CM.Gefaehrdungsbeurteilung.Services;

public sealed class StorageService
{
    public static string DataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CM", "Gefaehrdungsbeurteilung");
    private readonly string _dbPath = Path.Combine(DataDirectory, "beurteilungen.sqlite");
    private string ConnectionString => new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
    public string DatabasePath => _dbPath;

    public StorageService()
    {
        Directory.CreateDirectory(DataDirectory);
        using var db = Open();
        using var command = db.CreateCommand();
        command.CommandText = "PRAGMA user_version";
        int version = Convert.ToInt32(command.ExecuteScalar());
        if (version > 3) throw new InvalidOperationException("Die Datenbank stammt aus einer neueren Programmversion. Kein Schreibzugriff möglich.");
        if (version == 0)
        {
            // Bei späteren Schema-Upgrades erfolgt eine Sicherung vor der Migration.
            if (File.Exists(_dbPath) && new FileInfo(_dbPath).Length > 0)
                Backup("vor-schema-1");
            using var tx = db.BeginTransaction();
            using var create = db.CreateCommand();
            create.Transaction = tx;
            create.CommandText = """
                CREATE TABLE IF NOT EXISTS assessments (
                    id TEXT PRIMARY KEY, project_number TEXT NOT NULL, site_address TEXT NOT NULL,
                    area TEXT NOT NULL, work_description TEXT NOT NULL, selected_activities TEXT NOT NULL DEFAULT '', responsible TEXT NOT NULL,
                    status TEXT NOT NULL, updated_utc TEXT NOT NULL, reviewed_utc TEXT NULL
                );
                CREATE TABLE IF NOT EXISTS assessment_hazards (
                    id TEXT PRIMARY KEY, assessment_id TEXT NOT NULL REFERENCES assessments(id) ON DELETE CASCADE,
                    source_id TEXT NOT NULL, title TEXT NOT NULL, description TEXT NOT NULL,
                    measures TEXT NOT NULL, before_probability INTEGER NOT NULL, before_severity INTEGER NOT NULL,
                    after_probability INTEGER NOT NULL, after_severity INTEGER NOT NULL,
                    owner TEXT NOT NULL, due_date TEXT NULL, effectiveness_checked INTEGER NOT NULL
                );
                CREATE TABLE IF NOT EXISTS settings (key TEXT PRIMARY KEY, value TEXT NOT NULL);
                CREATE TABLE IF NOT EXISTS assessment_site_checks (
                    assessment_id TEXT NOT NULL REFERENCES assessments(id) ON DELETE CASCADE,
                    check_id TEXT NOT NULL, result TEXT NOT NULL, notes TEXT NOT NULL,
                    expert_clarification_documented INTEGER NOT NULL,
                    PRIMARY KEY (assessment_id,check_id)
                );
                PRAGMA user_version=3;
                """;
            create.ExecuteNonQuery();
            tx.Commit();
        }
        if (version == 1)
        {
            // Version-1-Bestandsdaten bleiben erhalten. Vollständige SQLite-Online-Sicherung vor Schemaänderung.
            var backup = Backup("vor-schema-2");
            using (var verify = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = backup, Mode = SqliteOpenMode.ReadOnly }.ToString()))
            {
                verify.Open();
                using var check = verify.CreateCommand();
                check.CommandText = "PRAGMA integrity_check";
                if (!string.Equals(Convert.ToString(check.ExecuteScalar()), "ok", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Sicherung vor Datenbankmigration ist unvollständig. Migration abgebrochen.");
            }
            using var tx = db.BeginTransaction();
            using var migrate = db.CreateCommand();
            migrate.Transaction = tx;
            migrate.CommandText = """
                CREATE TABLE assessment_site_checks (
                    assessment_id TEXT NOT NULL REFERENCES assessments(id) ON DELETE CASCADE,
                    check_id TEXT NOT NULL, result TEXT NOT NULL, notes TEXT NOT NULL,
                    expert_clarification_documented INTEGER NOT NULL,
                    PRIMARY KEY (assessment_id,check_id)
                );
                PRAGMA user_version=2;
                UPDATE assessments SET status='Erneute Prüfung erforderlich'
                WHERE status='Fachlich geprüft';
                """;
            migrate.ExecuteNonQuery();
            tx.Commit();
        }
        if (version == 1 || version == 2)
        {
            // Schema 3: Mehrfachauswahl von Tätigkeiten. Vor jedem Upgrade Snapshot prüfen.
            var backup = Backup("vor-schema-3");
            using (var verify = new SqliteConnection(new SqliteConnectionStringBuilder
                { DataSource = backup, Mode = SqliteOpenMode.ReadOnly }.ToString()))
            {
                verify.Open();
                using var check = verify.CreateCommand();
                check.CommandText = "PRAGMA integrity_check";
                if (!string.Equals(Convert.ToString(check.ExecuteScalar()), "ok", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Sicherung vor Schema 3 unvollständig. Migration abgebrochen.");
            }
            using var tx = db.BeginTransaction();
            using var migrate3 = db.CreateCommand();
            migrate3.Transaction = tx;
            migrate3.CommandText = """
                ALTER TABLE assessments ADD COLUMN selected_activities TEXT NOT NULL DEFAULT '';
                UPDATE assessments SET status='Erneute Prüfung erforderlich' WHERE status='Fachlich geprüft';
                PRAGMA user_version=3;
                """;
            migrate3.ExecuteNonQuery();
            tx.Commit();
        }
        EnsureDailyBackup();
    }

    private void EnsureDailyBackup()
    {
        var folder = Path.Combine(DataDirectory, "Backups");
        Directory.CreateDirectory(folder);
        string stamp = DateTime.Now.ToString("yyyyMMdd");
        if (!Directory.EnumerateFiles(folder, $"Backup-{stamp}-*-auto.sqlite").Any())
            Backup("auto");
        // Nur automatische Sicherungen begrenzen; manuelle und Update-Sicherungen bleiben erhalten.
        foreach (var old in Directory.EnumerateFiles(folder, "Backup-*-auto.sqlite")
                     .OrderByDescending(File.GetLastWriteTimeUtc).Skip(21))
            File.Delete(old);
    }

    private SqliteConnection Open()
    {
        var db = new SqliteConnection(ConnectionString);
        db.Open();
        using var pragma = db.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys=ON; PRAGMA busy_timeout=5000";
        pragma.ExecuteNonQuery();
        return db;
    }

    public string Backup(string reason = "manuell")
    {
        var folder = Path.Combine(DataDirectory, "Backups");
        Directory.CreateDirectory(folder);
        var sanitized = string.Concat(reason.Where(c => char.IsAsciiLetterOrDigit(c) || c == '-'));
        var file = Path.Combine(folder, $"Backup-{DateTime.Now:yyyyMMdd-HHmmss-fff}-{sanitized}.sqlite");
        using var source = Open();
        using var target = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = file }.ToString());
        target.Open();
        source.BackupDatabase(target);
        return file;
    }

    public List<Assessment> All()
    {
        var items = new List<Assessment>();
        using var db = Open();
        using var c = db.CreateCommand();
        c.CommandText = "SELECT id,project_number,site_address,area,work_description,responsible,status,updated_utc,reviewed_utc,selected_activities FROM assessments ORDER BY updated_utc DESC";
        using var reader = c.ExecuteReader();
        while (reader.Read()) items.Add(ReadAssessment(reader));
        return items;
    }

    public Assessment? Get(string id)
    {
        using var db = Open();
        using var c = db.CreateCommand();
        c.CommandText = "SELECT id,project_number,site_address,area,work_description,responsible,status,updated_utc,reviewed_utc,selected_activities FROM assessments WHERE id=$id";
        c.Parameters.AddWithValue("$id", id);
        using var reader = c.ExecuteReader();
        if (!reader.Read()) return null;
        var item = ReadAssessment(reader);
        reader.Close();
        using var h = db.CreateCommand();
        h.CommandText = "SELECT id,source_id,title,description,measures,before_probability,before_severity,after_probability,after_severity,owner,due_date,effectiveness_checked FROM assessment_hazards WHERE assessment_id=$id ORDER BY rowid";
        h.Parameters.AddWithValue("$id", id);
        using var hr = h.ExecuteReader();
        while (hr.Read()) item.Hazards.Add(new AssessmentHazard
        {
            Id = hr.GetString(0), SourceId = hr.GetString(1), Title = hr.GetString(2), Description = hr.GetString(3),
            Measures = hr.GetString(4), BeforeProbability = hr.GetInt32(5), BeforeSeverity = hr.GetInt32(6),
            AfterProbability = hr.GetInt32(7), AfterSeverity = hr.GetInt32(8), Owner = hr.GetString(9),
            DueDate = hr.IsDBNull(10) ? null : DateTime.Parse(hr.GetString(10), CultureInfo.InvariantCulture),
            EffectivenessChecked = hr.GetInt32(11) == 1
        });
        hr.Close();
        using var sc = db.CreateCommand();
        sc.CommandText = "SELECT check_id,result,notes,expert_clarification_documented FROM assessment_site_checks WHERE assessment_id=$id";
        sc.Parameters.AddWithValue("$id", id);
        using var sr = sc.ExecuteReader();
        while (sr.Read())
        {
            var existing = SiteScreeningService.Defaults.FirstOrDefault(x => x.Id == sr.GetString(0));
            if (existing is null) continue;
            item.SiteChecks.Add(new SiteCheck
            {
                Id = existing.Id, Area = existing.Area, Question = existing.Question,
                RelatedSources = existing.RelatedSources, RequiresExpertClarification = existing.RequiresExpertClarification,
                Result = sr.GetString(1), Notes = sr.GetString(2), ExpertClarificationDocumented = sr.GetInt32(3) == 1
            });
        }
        SiteScreeningService.EnsureDefaults(item);
        return item;
    }

    private static Assessment ReadAssessment(SqliteDataReader r) => new()
    {
        Id = r.GetString(0), ProjectNumber = r.GetString(1), SiteAddress = r.GetString(2),
        Area = r.GetString(3), WorkDescription = r.GetString(4), Responsible = r.GetString(5),
        Status = r.GetString(6), UpdatedUtc = DateTime.Parse(r.GetString(7), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
        ReviewedUtc = r.IsDBNull(8) ? null : DateTime.Parse(r.GetString(8), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
        SelectedActivities = r.GetString(9)
    };

    public void Save(Assessment a)
    {
        a.UpdatedUtc = DateTime.UtcNow;
        using var db = Open();
        using var tx = db.BeginTransaction();
        using var c = db.CreateCommand();
        c.Transaction = tx;
        c.CommandText = """
            INSERT INTO assessments(id,project_number,site_address,area,work_description,responsible,status,updated_utc,reviewed_utc,selected_activities)
            VALUES($id,$number,$site,$area,$work,$responsible,$status,$updated,$reviewed,$activities)
            ON CONFLICT(id) DO UPDATE SET project_number=excluded.project_number,site_address=excluded.site_address,
            area=excluded.area,work_description=excluded.work_description,responsible=excluded.responsible,
            status=excluded.status,updated_utc=excluded.updated_utc,reviewed_utc=excluded.reviewed_utc,
            selected_activities=excluded.selected_activities
            """;
        c.Parameters.AddWithValue("$id", a.Id);
        c.Parameters.AddWithValue("$number", a.ProjectNumber);
        c.Parameters.AddWithValue("$site", a.SiteAddress);
        c.Parameters.AddWithValue("$area", a.Area);
        c.Parameters.AddWithValue("$work", a.WorkDescription);
        c.Parameters.AddWithValue("$activities", a.SelectedActivities);
        c.Parameters.AddWithValue("$responsible", a.Responsible);
        c.Parameters.AddWithValue("$status", a.Status);
        c.Parameters.AddWithValue("$updated", a.UpdatedUtc.ToString("O"));
        c.Parameters.AddWithValue("$reviewed", (object?)a.ReviewedUtc?.ToString("O") ?? DBNull.Value);
        c.ExecuteNonQuery();
        using var delete = db.CreateCommand();
        delete.Transaction = tx;
        delete.CommandText = "DELETE FROM assessment_hazards WHERE assessment_id=$id";
        delete.Parameters.AddWithValue("$id", a.Id);
        delete.ExecuteNonQuery();
        foreach (var hazard in a.Hazards)
        {
            using var h = db.CreateCommand();
            h.Transaction = tx;
            h.CommandText = """
                INSERT INTO assessment_hazards(id,assessment_id,source_id,title,description,measures,
                 before_probability,before_severity,after_probability,after_severity,owner,due_date,effectiveness_checked)
                VALUES($id,$assessment,$source,$title,$description,$measures,$bp,$bs,$ap,$as,$owner,$due,$checked)
                """;
            h.Parameters.AddWithValue("$id", hazard.Id);
            h.Parameters.AddWithValue("$assessment", a.Id);
            h.Parameters.AddWithValue("$source", hazard.SourceId);
            h.Parameters.AddWithValue("$title", hazard.Title);
            h.Parameters.AddWithValue("$description", hazard.Description);
            h.Parameters.AddWithValue("$measures", hazard.Measures);
            h.Parameters.AddWithValue("$bp", hazard.BeforeProbability);
            h.Parameters.AddWithValue("$bs", hazard.BeforeSeverity);
            h.Parameters.AddWithValue("$ap", hazard.AfterProbability);
            h.Parameters.AddWithValue("$as", hazard.AfterSeverity);
            h.Parameters.AddWithValue("$owner", hazard.Owner);
            h.Parameters.AddWithValue("$due", (object?)hazard.DueDate?.ToString("yyyy-MM-dd") ?? DBNull.Value);
            h.Parameters.AddWithValue("$checked", hazard.EffectivenessChecked ? 1 : 0);
            h.ExecuteNonQuery();
        }
        // Auch bisher ungeklärte Antworten persistieren, damit eine Prüfung nach Neustart reproduzierbar ist.
        SiteScreeningService.EnsureDefaults(a);
        using var deleteChecks = db.CreateCommand();
        deleteChecks.Transaction = tx;
        deleteChecks.CommandText = "DELETE FROM assessment_site_checks WHERE assessment_id=$id";
        deleteChecks.Parameters.AddWithValue("$id", a.Id);
        deleteChecks.ExecuteNonQuery();
        foreach (var check in a.SiteChecks)
        {
            using var ins = db.CreateCommand();
            ins.Transaction = tx;
            ins.CommandText = """
                INSERT INTO assessment_site_checks (assessment_id,check_id,result,notes,expert_clarification_documented)
                VALUES($aid,$cid,$res,$notes,$proof)
                """;
            ins.Parameters.AddWithValue("$aid", a.Id);
            ins.Parameters.AddWithValue("$cid", check.Id);
            ins.Parameters.AddWithValue("$res", check.Result);
            ins.Parameters.AddWithValue("$notes", check.Notes);
            ins.Parameters.AddWithValue("$proof", check.ExpertClarificationDocumented ? 1 : 0);
            ins.ExecuteNonQuery();
        }
        tx.Commit();
    }

    public void Delete(string id)
    {
        using var db = Open();
        using var c = db.CreateCommand();
        c.CommandText = "DELETE FROM assessments WHERE id=$id";
        c.Parameters.AddWithValue("$id", id);
        c.ExecuteNonQuery();
    }

    public string GetSetting(string key)
    {
        using var db = Open();
        using var c = db.CreateCommand();
        c.CommandText = "SELECT value FROM settings WHERE key=$key";
        c.Parameters.AddWithValue("$key", key);
        return c.ExecuteScalar() as string ?? "";
    }

    public void SetSetting(string key, string value)
    {
        using var db = Open();
        using var c = db.CreateCommand();
        c.CommandText = "INSERT INTO settings(key,value) VALUES($key,$value) ON CONFLICT(key) DO UPDATE SET value=excluded.value";
        c.Parameters.AddWithValue("$key", key);
        c.Parameters.AddWithValue("$value", value);
        c.ExecuteNonQuery();
    }
}
