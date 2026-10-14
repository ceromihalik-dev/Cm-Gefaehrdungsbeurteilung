using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CM.Gefaehrdungsbeurteilung.Models;

namespace CM.Gefaehrdungsbeurteilung.Services;

/// <summary>
/// Windows-Druckdialog. Eine PDF-Datei kann über den systemeigenen Drucker
/// "Microsoft Print to PDF" erzeugt werden. Kein direkter PDF-Dateiexport.
/// </summary>
public static class ReportService
{
    private static readonly Brush Ink = new SolidColorBrush(Color.FromRgb(32, 55, 68));
    private static readonly Brush Blue = new SolidColorBrush(Color.FromRgb(20, 99, 116));
    private static readonly Brush Muted = new SolidColorBrush(Color.FromRgb(90, 108, 119));

    private static Paragraph Para(string text, int pt = 10, bool bold = false, Brush? color = null)
    {
        var p = new Paragraph(new Run(text))
        {
            FontSize = pt,
            FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
            Foreground = color ?? Ink,
            Margin = new Thickness(0, 3, 0, 8)
        };
        return p;
    }

    private static void Field(FlowDocument doc, string title, string value)
    {
        var p = new Paragraph { Margin = new Thickness(0, 3, 0, 7), FontSize = 10 };
        p.Inlines.Add(new Bold(new Run(title + ": ")) { Foreground = Ink });
        p.Inlines.Add(new Run(string.IsNullOrWhiteSpace(value) ? "—" : value) { Foreground = Ink });
        doc.Blocks.Add(p);
    }

    private static TableCell Cell(string text, bool bold = false)
    {
        var p = Para(text, 8, bold);
        p.Margin = new Thickness(3, 2, 3, 3);
        return new TableCell(p) { Padding = new Thickness(3), BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(0, 0, 0, 0.5) };
    }

    private static void AddActionPlan(FlowDocument doc, Assessment assessment)
    {
        doc.Blocks.Add(Para("Maßnahmenübersicht / Verantwortlichkeiten", 14, true, Blue));
        var table = new Table { CellSpacing = 0 };
        table.Columns.Add(new TableColumn { Width = new GridLength(2.4, GridUnitType.Star) });
        table.Columns.Add(new TableColumn { Width = new GridLength(3.8, GridUnitType.Star) });
        table.Columns.Add(new TableColumn { Width = new GridLength(1.4, GridUnitType.Star) });
        table.Columns.Add(new TableColumn { Width = new GridLength(1.4, GridUnitType.Star) });
        table.Columns.Add(new TableColumn { Width = new GridLength(1.2, GridUnitType.Star) });
        var group = new TableRowGroup();
        var header = new TableRow { Background = new SolidColorBrush(Color.FromRgb(230, 241, 244)) };
        foreach (var name in new[] { "Gefährdung", "Maßnahmen (Kurzüberblick)", "Zuständig", "Frist", "Kontrolle" })
            header.Cells.Add(Cell(name, true));
        group.Rows.Add(header);
        foreach (var hazard in assessment.Hazards)
        {
            var row = new TableRow();
            foreach (var value in new[]
            {
                hazard.Title, hazard.Measures, hazard.Owner,
                hazard.DueDate?.ToString("dd.MM.yyyy") ?? "offen",
                hazard.EffectivenessChecked ? "ja" : "offen"
            }) row.Cells.Add(Cell(string.IsNullOrWhiteSpace(value) ? "—" : value));
            group.Rows.Add(row);
        }
        table.RowGroups.Add(group);
        doc.Blocks.Add(table);
        doc.Blocks.Add(Para("Maßnahmen sind vor Arbeitsbeginn umzusetzen bzw. hinsichtlich ihrer Wirksamkeit zu überprüfen. Offene Maßnahmen gesondert behandeln.", 9, false, Muted));
    }

    public static void Print(Assessment assessment, string company, string companyAddress, string logoPath)
    {
        var dlg = new PrintDialog();
        if (dlg.ShowDialog() != true) return;
        var pageWidth = dlg.PrintableAreaWidth;
        var pageHeight = dlg.PrintableAreaHeight;
        if (pageWidth <= 0 || pageHeight <= 0) throw new InvalidOperationException("Kein nutzbarer Druckbereich verfügbar.");

        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI"),
            PageWidth = pageWidth, PageHeight = pageHeight,
            PagePadding = new Thickness(30, 24, 30, 25),
            ColumnWidth = pageWidth - 60,
            Background = Brushes.White
        };
        if (!string.IsNullOrWhiteSpace(logoPath) && File.Exists(logoPath))
        {
            try
            {
                var imageSource = new BitmapImage();
                imageSource.BeginInit();
                imageSource.CacheOption = BitmapCacheOption.OnLoad;
                imageSource.UriSource = new Uri(logoPath, UriKind.Absolute);
                imageSource.DecodePixelWidth = 500;
                imageSource.EndInit();
                imageSource.Freeze();
                doc.Blocks.Add(new BlockUIContainer(new Image
                {
                    Source = imageSource, MaxWidth = 190, MaxHeight = 66,
                    Stretch = Stretch.Uniform, HorizontalAlignment = HorizontalAlignment.Left
                }) { Margin = new Thickness(0, 0, 0, 8) });
            }
            catch (Exception)
            {
                doc.Blocks.Add(Para("Hinweis: Das Firmenlogo konnte nicht geladen werden.", 8, false, Muted));
            }
        }
        doc.Blocks.Add(Para(company, 11, true, Blue));
        if (!string.IsNullOrWhiteSpace(companyAddress)) doc.Blocks.Add(Para(companyAddress, 9, false, Muted));
        doc.Blocks.Add(Para("GEFÄHRDUNGSBEURTEILUNG", 19, true, Blue));
        doc.Blocks.Add(Para(assessment.Status == "Fachlich geprüft"
            ? "STATUS: Fachlich geprüft – objektspezifische Nachweise beachten"
            : "STATUS: ENTWURF – NICHT ALS FREIGABE VERWENDEN", 11, true,
            assessment.Status == "Fachlich geprüft" ? Blue : Brushes.DarkRed));
        Field(doc, "Projektnummer", assessment.ProjectNumber);
        Field(doc, "Objekt", assessment.SiteAddress);
        Field(doc, "Tätigkeitsbereich", assessment.Area);
        var activities = ActivityService.Selected(assessment);
        Field(doc, "Ausgewählte Tätigkeiten", activities.Count > 0
            ? string.Join("; ", activities.Select(a => a.Title)) : "nicht erfasst");
        Field(doc, "Arbeiten", assessment.WorkDescription);
        Field(doc, "Verantwortliche Person", assessment.Responsible);
        Field(doc, "Bearbeitungsstand", assessment.UpdatedUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm"));
        if (assessment.ReviewedUtc.HasValue)
            Field(doc, "Prüfung dokumentiert", assessment.ReviewedUtc.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm"));

        doc.Blocks.Add(Para("Objektspezifischer Sicherheitscheck", 14, true, Blue));
        foreach (var check in SiteScreeningService.Applicable(assessment))
        {
            doc.Blocks.Add(Para(check.Question, 10, true));
            doc.Blocks.Add(Para($"Antwort: {check.Result}.  Fachkundige Klärung dokumentiert: {(check.ExpertClarificationDocumented ? "ja" : "nein")}", 9));
            if (!string.IsNullOrWhiteSpace(check.Notes)) doc.Blocks.Add(Para(check.Notes, 9));
        }
        var openChecks = SiteScreeningService.BlockingReasons(assessment);
        if (openChecks.Count > 0)
            doc.Blocks.Add(Para($"OFFENE PUNKTE IM OBJEKT-CHECK: {openChecks.Count}. Keine automatische Arbeitsfreigabe.", 10, true, Brushes.DarkRed));
        AddActionPlan(doc, assessment);
        doc.Blocks.Add(Para("Gefährdungen und Schutzmaßnahmen – Einzeldarstellung", 14, true, Blue));
        int index = 0;
        foreach (var h in assessment.Hazards)
        {
            index++;
            var sec = new Section { BreakPageBefore = false };
            sec.Blocks.Add(Para($"{index}. {h.Title}", 12, true, Blue));
            sec.Blocks.Add(Para(h.Description));
            sec.Blocks.Add(Para("Schutzmaßnahmen", 10, true));
            sec.Blocks.Add(Para(h.Measures));
            sec.Blocks.Add(Para($"Orientierender Risikowert: vorher {h.RiskBefore}  /  nachher {h.RiskAfter}", 9, false, Muted));
            sec.Blocks.Add(Para($"Zuständig: {(string.IsNullOrWhiteSpace(h.Owner) ? "—" : h.Owner)}  |  Frist: {(h.DueDate.HasValue ? h.DueDate.Value.ToString("dd.MM.yyyy") : "—")}", 9));
            sec.Blocks.Add(Para("Wirksamkeit kontrolliert: " + (h.EffectivenessChecked ? "ja" : "nein / offen"), 9));
            sec.Blocks.Add(new Paragraph(new Run(" ")) { BorderBrush = new SolidColorBrush(Color.FromRgb(216, 228, 234)), BorderThickness = new Thickness(0, 0, 0, 0.7), Margin = new Thickness(0, 0, 0, 11) });
            doc.Blocks.Add(sec);
        }
        if (assessment.Hazards.Count == 0)
            doc.Blocks.Add(Para("Keine Gefährdungen erfasst. Beurteilung ist unvollständig.", 11, true, Brushes.DarkRed));
        doc.Blocks.Add(Para("Dokumentation der fachlichen Prüfung", 13, true, Blue));
        Field(doc, "Verantwortliche fachkundige Person", assessment.Responsible);
        Field(doc, "Prüfdatum", assessment.ReviewedUtc?.ToLocalTime().ToString("dd.MM.yyyy") ?? "nicht dokumentiert");
        doc.Blocks.Add(Para("Ort / Datum: _________________________________________", 10));
        doc.Blocks.Add(Para("Unterschrift (für die betriebliche Dokumentation): _________________________________________", 10));
        doc.Blocks.Add(Para("Wichtiger Hinweis", 11, true));
        doc.Blocks.Add(Para("Dieses Dokument gibt den eingegebenen Bearbeitungsstand wieder. Die aufgeführten Beispielmaßnahmen sind nicht vollständig und müssen von fachkundigen Personen an die konkrete Tätigkeit, örtlichen Bedingungen und gültigen Vorschriften angepasst werden.", 9, false, Muted));

        dlg.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator,
            "Gefährdungsbeurteilung " + assessment.ProjectNumber);
    }
}
