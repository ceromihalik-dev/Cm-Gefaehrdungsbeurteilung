using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CM.Gefaehrdungsbeurteilung.Models;
using CM.Gefaehrdungsbeurteilung.Services;

namespace CM.Gefaehrdungsbeurteilung;

public partial class MainWindow : Window
{
    private StorageService _storage = null!;
    private Assessment? _current;
    private AssessmentHazard? _selectedHazard;
    private ReleaseInfo? _latestRelease;
    private bool _loading;
    private bool _installingUpdate;
    private List<ActivityChoice> _activityChoices = new();

    public MainWindow()
    {
        InitializeComponent();
        var installedVersion = UpdateService.CurrentVersion;
        var displayVersion = $"{installedVersion.Major}.{installedVersion.Minor}.{installedVersion.Build}";
        SidebarVersionText.Text = $"Version {displayVersion}  •  Entwicklungsstand";
        var fileVersion = FileVersionInfo.GetVersionInfo(typeof(MainWindow).Assembly.Location);
        InstalledVersionText.Text = $"Installiert: {displayVersion} / Build {fileVersion.FilePrivatePart:D4}";
        try { _storage = new StorageService(); }
        catch (Exception ex)
        {
            MessageBox.Show("Lokale Datenbank konnte nicht geöffnet werden.\n\n" + ex.Message,
                "Speicherfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            Dispatcher.BeginInvoke(new Action(Close));
            return;
        }
        CatalogGrid.ItemsSource = CatalogService.Entries;
        EnsureDefaultUpdateSource();
        LoadSettings();
        LoadDashboard();
        UpdateCatalog();
        Loaded += MainWindow_Loaded;
    }

    private void EnsureDefaultUpdateSource()
    {
        // Vorhandene individuelle Einstellungen nicht überschreiben.
        if (string.IsNullOrWhiteSpace(_storage.GetSetting("repo.owner")))
            _storage.SetSetting("repo.owner", "ceromihalik-dev");
        if (string.IsNullOrWhiteSpace(_storage.GetSetting("repo.name")))
            _storage.SetSetting("repo.name", "Cm-Gefaehrdungsbeurteilung");
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (_storage.GetSetting("updates.autocheck") == "0") return;
        var owner = _storage.GetSetting("repo.owner");
        var repo = _storage.GetSetting("repo.name");
        if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repo)) return;
        if (DateTime.TryParse(_storage.GetSetting("updates.lastCheckUtc"), out var last) &&
            DateTime.UtcNow - last.ToUniversalTime() < TimeSpan.FromDays(1)) return;
        try
        {
            _latestRelease = await UpdateService.GetLatestAsync(owner, repo);
            _storage.SetSetting("updates.lastCheckUtc", DateTime.UtcNow.ToString("O"));
            if (_latestRelease.Version > UpdateService.CurrentVersion &&
                MessageBox.Show($"Eine neue Version {_latestRelease.Version} ist verfügbar. Updateinformationen anzeigen?",
                    "Neues Update", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
            {
                Updates_Click(sender, e);
                UpdateStatus.Text = $"Neue Version {_latestRelease.Version} verfügbar.";
                UpdateNotes.Text = _latestRelease.Notes;
                OpenReleaseButton.IsEnabled = true;
                InstallUpdateButton.IsEnabled = _latestRelease.HasInstaller &&
                    UpdateService.CanAutoInstall;
            }
        }
        catch (Exception ex) { UpdateStatus.Text = "Automatische Updateprüfung fehlgeschlagen: " + ex.Message; }
    }

    private void ShowView(FrameworkElement panel, string title)
    {
        if (_current is not null && EditorView.Visibility == Visibility.Visible && !ReferenceEquals(panel, EditorView))
            SaveCurrent(); // Bearbeitungsstand bleibt beim Navigieren erhalten.
        FrameworkElement[] views = { DashboardView, AssessmentsView, EditorView, CatalogView, SettingsView, UpdatesView };
        foreach (var view in views)
            view.Visibility = ReferenceEquals(view, panel) ? Visibility.Visible : Visibility.Collapsed;
        MainTitle.Text = title;
    }

    private void LoadDashboard()
    {
        var all = _storage.All();
        DashboardTotal.Text = all.Count.ToString();
        DashboardDrafts.Text = all.Count(a => a.Status != "Fachlich geprüft").ToString();
        DashboardReviewed.Text = all.Count(a => a.Status == "Fachlich geprüft").ToString();
        RecentGrid.ItemsSource = all.Take(8).ToList();
        AssessmentsGrid.ItemsSource = all;
    }

    private void Dashboard_Click(object sender, RoutedEventArgs e) { ShowView(DashboardView, "Übersicht"); LoadDashboard(); }
    private void Assessments_Click(object sender, RoutedEventArgs e) { ShowView(AssessmentsView, "Gefährdungsbeurteilungen"); LoadDashboard(); }
    private void Catalog_Click(object sender, RoutedEventArgs e) => ShowView(CatalogView, "Gefährdungskatalog");
    private void Settings_Click(object sender, RoutedEventArgs e) { LoadSettings(); ShowView(SettingsView, "Einstellungen"); }
    private void Updates_Click(object sender, RoutedEventArgs e)
    {
        UpdateSourceText.Text = "Updatequelle: " + (string.IsNullOrWhiteSpace(_storage.GetSetting("repo.owner"))
            ? "noch nicht eingerichtet" : $"github.com/{_storage.GetSetting("repo.owner")}/{_storage.GetSetting("repo.name")}");
        ShowView(UpdatesView, "Updates");
    }

    private void New_Click(object sender, RoutedEventArgs e)
    {
        if (_current is not null && EditorView.Visibility == Visibility.Visible) SaveCurrent();
        _current = new Assessment();
        SiteScreeningService.EnsureDefaults(_current);
        EditAssessment(_current);
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (AssessmentsGrid.SelectedItem is not Assessment a) { Hint("Bitte zunächst eine Beurteilung auswählen."); return; }
        OpenById(a.Id);
    }

    private void AssessmentGrid_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is DataGrid grid && grid.SelectedItem is Assessment a) OpenById(a.Id);
    }

    private void OpenById(string id)
    {
        _current = _storage.Get(id);
        if (_current != null) EditAssessment(_current);
    }

    private void EditAssessment(Assessment assessment)
    {
        _loading = true;
        _selectedHazard = null;
        ProjectNumberBox.Text = assessment.ProjectNumber;
        SiteAddressBox.Text = assessment.SiteAddress;
        AreaBox.SelectedIndex = assessment.Area == "Brandschadensanierung" ? 1 :
            assessment.Area == "Allgemeine Sanierungsarbeiten" ? 2 : 0;
        WorkDescriptionBox.Text = assessment.WorkDescription;
        var selectedActivities = ActivityService.Ids(assessment);
        _activityChoices = ActivityService.Entries.Select(d => new ActivityChoice(d, selectedActivities.Contains(d.Id))).ToList();
        ActivityChoicesList.ItemsSource = _activityChoices;
        ActivityInfo.Text = $"{selectedActivities.Count} Tätigkeit(en) ausgewählt";
        ResponsibleBox.Text = assessment.Responsible;
        SiteScreeningService.EnsureDefaults(assessment);
        SiteChecksList.ItemsSource = SiteScreeningService.Applicable(assessment);
        SelectedHazardsList.ItemsSource = assessment.Hazards;
        SelectedHazardsList.SelectedItem = null;
        ClearHazardDetails();
        EditorSavedInfo.Text = "Status: " + assessment.Status;
        ReviewConfirmation.IsChecked = false;
        _loading = false;
        UpdateCatalog();
        UpdateReviewSummary();
        EditorTabs.SelectedIndex = 0;
        ShowView(EditorView, "Beurteilung bearbeiten");
    }

    private void CaptureBasics()
    {
        if (_current is null) return;
        string area = AreaBox.SelectedIndex == 1 ? "Brandschadensanierung" :
            AreaBox.SelectedIndex == 2 ? "Allgemeine Sanierungsarbeiten" : "Wasserschadensanierung";
        if (_current.ProjectNumber != ProjectNumberBox.Text.Trim() || _current.SiteAddress != SiteAddressBox.Text.Trim() ||
            _current.Area != area || _current.WorkDescription != WorkDescriptionBox.Text.Trim() || _current.Responsible != ResponsibleBox.Text.Trim())
            MarkDraft();
        _current.ProjectNumber = ProjectNumberBox.Text.Trim();
        _current.SiteAddress = SiteAddressBox.Text.Trim();
        if (_current.Area != area)
        {
            _current.Area = area;
            if (SiteChecksList is not null)
                SiteChecksList.ItemsSource = SiteScreeningService.Applicable(_current);
        }
        _current.WorkDescription = WorkDescriptionBox.Text.Trim();
        var activityIds = _activityChoices.Where(x => x.IsSelected).Select(x => x.Id).ToArray();
        var activities = string.Join(",", activityIds);
        if (_current.SelectedActivities != activities)
        {
            MarkDraft();
            _current.SelectedActivities = activities;
            SiteChecksList.ItemsSource = SiteScreeningService.Applicable(_current);
        }
        _current.Responsible = ResponsibleBox.Text.Trim();
    }

    private void MarkDraft()
    {
        if (_current is null) return;
        if (_current.Status == "Fachlich geprüft" || _current.Status == "Erneute Prüfung erforderlich")
        {
            _current.Status = "Entwurf";
            _current.ReviewedUtc = null;
            ReviewConfirmation.IsChecked = false;
        }
    }

    private void SaveSelectedHazardFields()
    {
        if (_loading || _selectedHazard is null) return;
        var h = _selectedHazard;
        string title = HazardTitleBox.Text.Trim();
        string description = HazardDescriptionBox.Text.Trim();
        string measures = HazardMeasuresBox.Text.Trim();
        string owner = HazardOwnerBox.Text.Trim();
        int bp = Math.Max(1, BeforePBox.SelectedIndex + 1);
        int bs = Math.Max(1, BeforeSBox.SelectedIndex + 1);
        int ap = Math.Max(1, AfterPBox.SelectedIndex + 1);
        int afs = Math.Max(1, AfterSBox.SelectedIndex + 1);
        var due = HazardDuePicker.SelectedDate;
        bool checkedValue = EffectivenessCheck.IsChecked == true;
        if (h.Title != title || h.Description != description || h.Measures != measures || h.Owner != owner ||
            h.BeforeProbability != bp || h.BeforeSeverity != bs || h.AfterProbability != ap ||
            h.AfterSeverity != afs || h.DueDate != due || h.EffectivenessChecked != checkedValue)
            MarkDraft();
        h.Title = title; h.Description = description; h.Measures = measures; h.Owner = owner;
        h.BeforeProbability = bp; h.BeforeSeverity = bs; h.AfterProbability = ap; h.AfterSeverity = afs;
        h.DueDate = due; h.EffectivenessChecked = checkedValue;
    }

    private void SaveCurrent()
    {
        if (_current is null) return;
        CaptureBasics();
        SaveSelectedHazardFields();
        _storage.Save(_current);
        EditorSavedInfo.Text = "Gespeichert: " + DateTime.Now.ToString("HH:mm:ss") + "  •  " + _current.Status;
        UpdateReviewSummary();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try { SaveCurrent(); Hint("Der Bearbeitungsstand wurde lokal gespeichert."); }
        catch (Exception ex) { ShowError(ex); }
    }

    private void Print_Click(object sender, RoutedEventArgs e)
    {
        if (_current is null) return;
        try
        {
            SaveCurrent();
            ReportService.Print(_current, _storage.GetSetting("company.name"), _storage.GetSetting("company.address"), _storage.GetSetting("company.logo"));
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void Area_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || CatalogList is null) return;
        if (_current is not null) CaptureBasics();
        UpdateCatalog();
        UpdateReviewSummary();
    }

    private void Activity_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading || _current is null || SiteChecksList is null) return;
        CaptureBasics();
        ActivityInfo.Text = $"{_activityChoices.Count(x => x.IsSelected)} Tätigkeit(en) ausgewählt";
        UpdateCatalog();
        UpdateReviewSummary();
    }

    private void AddActivitySuggestions_Click(object sender, RoutedEventArgs e)
    {
        if (_current is null) return;
        CaptureBasics();
        SaveSelectedHazardFields();
        var suggestions = ActivityService.Suggestions(_current);
        if (suggestions.Count == 0)
        {
            Hint("Keine neuen Vorschläge. Bitte Tätigkeiten wählen oder bereits hinzugefügte Einträge prüfen.");
            return;
        }
        if (MessageBox.Show($"{suggestions.Count} passende Gefährdungen zur Prüfung übernehmen?\n\n" +
            string.Join("\n", suggestions.Select(x => "• " + x.Title)) +
            "\n\nDie Beispiele müssen überprüft und bei Bedarf angepasst werden.",
            "Vorschläge übernehmen", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        foreach (var entry in suggestions)
            _current.Hazards.Add(new AssessmentHazard
            {
                SourceId = entry.Id, Title = entry.Title, Description = entry.Description,
                Measures = entry.SuggestedMeasures, Owner = _current.Responsible
            });
        MarkDraft();
        UpdateReviewSummary();
        Hint($"{suggestions.Count} Vorschläge übernommen. Bitte Gefährdungen und Maßnahmen prüfen.");
    }

    private void UpdateCatalog()
    {
        if (CatalogList is null) return;
        string area = AreaBox.SelectedIndex == 1 ? "Brandschadensanierung" :
            AreaBox.SelectedIndex == 2 ? "Allgemeine Sanierungsarbeiten" : "Wasserschadensanierung";
        var areas = _current is not null ? ActivityService.RelevantAreas(_current) : new HashSet<string> { "Allgemein", area };
        CatalogList.ItemsSource = CatalogService.Entries.Where(c => areas.Contains(c.Area)).ToList();
    }

    private void NextToHazards_Click(object sender, RoutedEventArgs e)
    {
        CaptureBasics();
        EditorTabs.SelectedIndex = 1;
    }
    private void NextToHazardsFromScreening_Click(object sender, RoutedEventArgs e) => EditorTabs.SelectedIndex = 2;

    private void ScreeningUpdated()
    {
        if (_loading || _current is null) return;
        MarkDraft();
        UpdateReviewSummary();
    }
    private void Screening_CheckChanged(object sender, RoutedEventArgs e) => ScreeningUpdated();
    private void Screening_ComboChanged(object sender, SelectionChangedEventArgs e) => ScreeningUpdated();
    private void Screening_TextChanged(object sender, TextChangedEventArgs e) => ScreeningUpdated();

    private void AddHazard_Click(object sender, RoutedEventArgs e)
    {
        if (_current is null || CatalogList.SelectedItem is not CatalogEntry c) { Hint("Bitte einen Katalogeintrag auswählen."); return; }
        SaveSelectedHazardFields();
        if (_current.Hazards.Any(h => h.SourceId == c.Id))
        {
            Hint("Dieser Katalogeintrag ist in der Beurteilung bereits vorhanden.");
            return;
        }
        var h = new AssessmentHazard
        {
            SourceId = c.Id, Title = c.Title, Description = c.Description, Measures = c.SuggestedMeasures,
            Owner = ResponsibleBox.Text.Trim()
        };
        _current.Hazards.Add(h);
        MarkDraft();
        SelectedHazardsList.SelectedItem = h;
        UpdateReviewSummary();
    }

    private void RemoveHazard_Click(object sender, RoutedEventArgs e)
    {
        if (_current is null || SelectedHazardsList.SelectedItem is not AssessmentHazard h) return;
        if (MessageBox.Show($"Gefährdung '{h.Title}' aus dieser Beurteilung entfernen?", "Entfernen", MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        _current.Hazards.Remove(h);
        _selectedHazard = null;
        ClearHazardDetails();
        MarkDraft();
        UpdateReviewSummary();
    }

    private void SelectedHazard_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_loading) return;
        SaveSelectedHazardFields();
        _selectedHazard = SelectedHazardsList.SelectedItem as AssessmentHazard;
        _loading = true;
        ClearHazardDetails();
        if (_selectedHazard is AssessmentHazard h)
        {
            HazardDetailsPanel.IsEnabled = true;
            HazardTitleBox.Text = h.Title;
            HazardDescriptionBox.Text = h.Description;
            HazardMeasuresBox.Text = h.Measures;
            BeforePBox.SelectedIndex = Math.Clamp(h.BeforeProbability - 1, 0, 2);
            BeforeSBox.SelectedIndex = Math.Clamp(h.BeforeSeverity - 1, 0, 2);
            AfterPBox.SelectedIndex = Math.Clamp(h.AfterProbability - 1, 0, 2);
            AfterSBox.SelectedIndex = Math.Clamp(h.AfterSeverity - 1, 0, 2);
            HazardOwnerBox.Text = h.Owner;
            HazardDuePicker.SelectedDate = h.DueDate;
            EffectivenessCheck.IsChecked = h.EffectivenessChecked;
        }
        _loading = false;
    }

    private void ClearHazardDetails()
    {
        HazardDetailsPanel.IsEnabled = false;
        HazardTitleBox.Text = ""; HazardDescriptionBox.Text = ""; HazardMeasuresBox.Text = "";
        HazardOwnerBox.Text = ""; HazardDuePicker.SelectedDate = null; EffectivenessCheck.IsChecked = false;
        BeforePBox.SelectedIndex = 1; BeforeSBox.SelectedIndex = 1;
        AfterPBox.SelectedIndex = 0; AfterSBox.SelectedIndex = 1;
    }

    private void InsertStopTemplate_Click(object sender, RoutedEventArgs e)
    {
        if (_current is null || _selectedHazard is null) return;
        const string heading = "STOP-Prüfung (objektspezifisch ergänzen):";
        if (HazardMeasuresBox.Text.Contains(heading, StringComparison.Ordinal))
        {
            Hint("Die STOP-Struktur ist bereits vorhanden.");
            return;
        }
        string template = string.Join(Environment.NewLine, new[]
        {
            heading,
            "S – Substitution: prüfen / begründen: ...",
            "T – Technisch: ...",
            "O – Organisatorisch: ...",
            "P – Persönliche Schutzausrüstung: ..."
        });
        var existing = HazardMeasuresBox.Text.Trim();
        HazardMeasuresBox.Text = string.IsNullOrWhiteSpace(existing)
            ? template : existing + Environment.NewLine + Environment.NewLine + template;
        MarkDraft();
        EditorSavedInfo.Text = "STOP-Struktur ergänzt – fachlich ausfüllen und speichern.";
    }

    private void ApplyHazard_Click(object sender, RoutedEventArgs e)
    {
        SaveSelectedHazardFields();
        var selected = _selectedHazard;
        _loading = true;
        SelectedHazardsList.Items.Refresh();
        SelectedHazardsList.SelectedItem = selected;
        _loading = false;
        UpdateReviewSummary();
        EditorSavedInfo.Text = "Änderungen im Entwurf übernommen – bitte speichern.";
    }

    private void UpdateReviewSummary()
    {
        if (_current is null) return;
        int n = _current.Hazards.Count;
        int activities = ActivityService.Selected(_current).Count;
        int missing = _current.Hazards.Count(h => string.IsNullOrWhiteSpace(h.Measures) || string.IsNullOrWhiteSpace(h.Owner));
        int high = _current.Hazards.Count(h => h.RiskAfter >= 6);
        var blocking = SiteScreeningService.BlockingReasons(_current);
        ReviewSummary.Text = $"{activities} Tätigkeit(en) ausgewählt  •  {n} Gefährdung(en) berücksichtigt  •  {missing} ohne Maßnahmen oder Zuständigkeit  •  {high} mit hohem Restrisikowert  •  {blocking.Count} offene Punkte im Objekt-Check  •  Status: {_current.Status}";
    }

    private void Review_Click(object sender, RoutedEventArgs e)
    {
        if (_current is null) return;
        CaptureBasics();
        SaveSelectedHazardFields();
        var blocking = SiteScreeningService.BlockingReasons(_current);
        if (blocking.Count > 0)
        {
            MessageBox.Show("Vor der fachlichen Abschlussprüfung müssen folgende Punkte geklärt werden:\n\n" +
                string.Join("\n\n", blocking.Take(8)) + (blocking.Count > 8 ? $"\n\n… weitere {blocking.Count - 8} Punkte." : ""),
                "Objekt-Check unvollständig", MessageBoxButton.OK, MessageBoxImage.Warning);
            EditorTabs.SelectedIndex = 1;
            return;
        }
        if (string.IsNullOrWhiteSpace(_current.ProjectNumber) || string.IsNullOrWhiteSpace(_current.SiteAddress) ||
            string.IsNullOrWhiteSpace(_current.Responsible) || ActivityService.Selected(_current).Count == 0 || _current.Hazards.Count == 0 ||
            _current.Hazards.Any(h => string.IsNullOrWhiteSpace(h.Title) || string.IsNullOrWhiteSpace(h.Measures) || string.IsNullOrWhiteSpace(h.Owner)) ||
            _current.Hazards.Any(h => h.RiskAfter >= 6))
        {
            Hint("Prüfung noch nicht möglich: Projekt, Objekt, mindestens eine Tätigkeit, verantwortliche Person, Gefährdungen und Maßnahmen/Zuständigkeiten vervollständigen; hohe Restrisiken fachlich klären.");
            return;
        }
        if (_current.Hazards.Any(h => h.Measures.Contains("STOP-Prüfung (objektspezifisch ergänzen):", StringComparison.Ordinal) &&
            h.Measures.Contains("...", StringComparison.Ordinal)))
        {
            Hint("Die STOP-Struktur enthält noch Platzhalter. Bitte jede Kategorie konkret ausfüllen oder die Nichtanwendbarkeit fachlich begründen.");
            EditorTabs.SelectedIndex = 2;
            return;
        }
        if (ReviewConfirmation.IsChecked != true) { Hint("Die Bestätigung durch die verantwortliche fachkundige Person fehlt."); return; }
        _current.Status = "Fachlich geprüft";
        _current.ReviewedUtc = DateTime.UtcNow;
        SaveCurrent();
        Hint("Die fachliche Prüfung wurde mit Datum dokumentiert. Die Unterlagen müssen weiterhin objektbezogen verantwortet werden.");
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (AssessmentsGrid.SelectedItem is not Assessment a) return;
        if (MessageBox.Show($"Beurteilung '{a.ProjectNumber}' wirklich löschen? Ein Backup wird zuvor erstellt.",
            "Löschen bestätigen", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try { _storage.Backup("vor-loeschen"); _storage.Delete(a.Id); LoadDashboard(); }
        catch (Exception ex) { ShowError(ex); }
    }

    private void LoadSettings()
    {
        CompanyBox.Text = _storage.GetSetting("company.name");
        CompanyAddressBox.Text = _storage.GetSetting("company.address");
        LogoPathBox.Text = _storage.GetSetting("company.logo");
        RepoOwnerBox.Text = _storage.GetSetting("repo.owner");
        RepoNameBox.Text = _storage.GetSetting("repo.name");
        TrustedPublisherText.Text = UpdateService.CanAutoInstall
            ? "Verifizierter Herausgeber-Fingerabdruck: " + UpdateService.TrustedSignerThumbprint
            : "Nicht hinterlegt (Entwicklungsbuild): automatische Installation deaktiviert.";
        AutoCheckUpdatesCheckBox.IsChecked = _storage.GetSetting("updates.autocheck") != "0";
        DatabasePathText.Text = "Datenbank: " + _storage.DatabasePath;
    }

    private void ChooseLogo_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Firmenlogo auswählen",
            Filter = "Bilddateien (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp",
            CheckFileExists = true
        };
        if (dialog.ShowDialog() != true) return;
        try
        {
            // Feste Dateinamen verhindern, dass externe Dateien aufbewahrt werden müssen.
            var extension = Path.GetExtension(dialog.FileName).ToLowerInvariant();
            if (extension is not (".png" or ".jpg" or ".jpeg" or ".bmp"))
                throw new InvalidOperationException("Unbekanntes Bildformat.");
            if (new FileInfo(dialog.FileName).Length > 5 * 1024 * 1024)
                throw new InvalidOperationException("Logo darf maximal 5 MB groß sein.");
            var logoFolder = Path.Combine(StorageService.DataDirectory, "Firmendaten");
            Directory.CreateDirectory(logoFolder);
            var destination = Path.Combine(logoFolder, "Firmenlogo" + extension);
            if (!Path.GetFullPath(dialog.FileName).Equals(Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase))
                File.Copy(dialog.FileName, destination, true);
            LogoPathBox.Text = destination;
            Hint("Logo lokal übernommen. Zum Abschluss Einstellungen speichern.");
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _storage.SetSetting("company.name", CompanyBox.Text.Trim());
            _storage.SetSetting("company.address", CompanyAddressBox.Text.Trim());
            _storage.SetSetting("company.logo", LogoPathBox.Text.Trim());
            _storage.SetSetting("repo.owner", RepoOwnerBox.Text.Trim());
            _storage.SetSetting("repo.name", RepoNameBox.Text.Trim());
            _storage.SetSetting("updates.autocheck", AutoCheckUpdatesCheckBox.IsChecked == true ? "1" : "0");
            Hint("Einstellungen lokal gespeichert.");
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void Backup_Click(object sender, RoutedEventArgs e)
    {
        try { Hint("Datenbank gesichert unter:\n" + _storage.Backup()); }
        catch (Exception ex) { ShowError(ex); }
    }

    private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
    {
        CheckUpdateButton.IsEnabled = false;
        InstallUpdateButton.IsEnabled = false;
        OpenReleaseButton.IsEnabled = false;
        _latestRelease = null;
        UpdateNotes.Text = "";
        UpdateStatus.Text = "Suche nach einem Release ...";
        try
        {
            _latestRelease = await UpdateService.GetLatestAsync(_storage.GetSetting("repo.owner"), _storage.GetSetting("repo.name"));
            _storage.SetSetting("updates.lastCheckUtc", DateTime.UtcNow.ToString("O"));
            OpenReleaseButton.IsEnabled = true;
            bool newer = _latestRelease.Version > UpdateService.CurrentVersion;
            UpdateStatus.Text = newer ? $"Neue Version {_latestRelease.Version} verfügbar." : $"Kein neueres Release. Letzte Veröffentlichung: {_latestRelease.Version}.";
            UpdateNotes.Text = _latestRelease.Notes;
            InstallUpdateButton.IsEnabled = newer && _latestRelease.HasInstaller &&
                UpdateService.CanAutoInstall;
            if (newer && !InstallUpdateButton.IsEnabled)
                UpdateStatus.Text += " Automatische Installation erst nach Einrichtung signierter Releases verfügbar.";
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("Kein öffentliches GitHub-Release", StringComparison.Ordinal))
        {
            UpdateStatus.Text = "Noch kein veröffentlichtes GitHub Release. Für manuelle Testversionen unten auf GitHub-Testversionen (Artifacts) klicken.";
            UpdateNotes.Text = "";
        }
        catch (Exception ex) { UpdateStatus.Text = "Updateprüfung fehlgeschlagen: " + ex.Message; }
        finally { CheckUpdateButton.IsEnabled = true; }
    }

    private void OpenTestBuilds_Click(object sender, RoutedEventArgs e)
    {
        // Fester, vom Projekt definierter Link; niemals eine ungeprüfte URL aus dem Release übernehmen.
        const string url = "https://github.com/ceromihalik-dev/Cm-Gefaehrdungsbeurteilung/actions/workflows/windows-build.yml";
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            UpdateStatus.Text = "GitHub konnte nicht geöffnet werden: " + ex.Message;
        }
    }

    private void OpenRelease_Click(object sender, RoutedEventArgs e)
    {
        if (_latestRelease is null) return;
        if (Uri.TryCreate(_latestRelease.HtmlUrl, UriKind.Absolute, out var uri) && uri.Scheme == "https" && uri.Host == "github.com")
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
    }

    private async void InstallUpdate_Click(object sender, RoutedEventArgs e)
    {
        if (_latestRelease is null || _latestRelease.Version <= UpdateService.CurrentVersion) return;
        if (MessageBox.Show("Das Update wird heruntergeladen und geprüft. Danach wird das Installationsprogramm gestartet und diese Anwendung beendet. Vorhandene Daten bleiben erhalten. Fortfahren?",
            "Update installieren", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        InstallUpdateButton.IsEnabled = false;
        UpdateStatus.Text = "Lade Update und überprüfe SHA-256 sowie Herausgebersignatur ...";
        try
        {
            if (_current != null) SaveCurrent();
            _storage.Backup("vor-update");
            string installer = await UpdateService.DownloadVerifiedInstallerAsync(_latestRelease, UpdateService.TrustedSignerThumbprint);
            if (Process.Start(new ProcessStartInfo(installer) { UseShellExecute = true, Arguments = "/CLOSEAPPLICATIONS /NORESTART" }) is null)
                throw new InvalidOperationException("Installationsprogramm konnte nicht gestartet werden.");
            _installingUpdate = true;
            Application.Current.Shutdown();
        }
        catch (Exception ex) { UpdateStatus.Text = "Update abgebrochen: " + ex.Message; InstallUpdateButton.IsEnabled = true; }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_installingUpdate && _storage != null && _current != null && EditorView.Visibility == Visibility.Visible)
        {
            try { SaveCurrent(); }
            catch (Exception ex)
            {
                MessageBox.Show("Speichern vor Beenden fehlgeschlagen: " + ex.Message + "\nBitte die Daten prüfen.", "Speicherfehler");
                e.Cancel = true;
            }
        }
        base.OnClosing(e);
    }

    private static void Hint(string message) => MessageBox.Show(message, "CM Gefährdungsbeurteilung", MessageBoxButton.OK, MessageBoxImage.Information);
    private static void ShowError(Exception ex) => MessageBox.Show(ex.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
}
