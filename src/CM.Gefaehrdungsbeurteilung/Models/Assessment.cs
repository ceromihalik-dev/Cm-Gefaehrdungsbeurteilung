using System.Collections.ObjectModel;

namespace CM.Gefaehrdungsbeurteilung.Models;

public sealed class Assessment
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ProjectNumber { get; set; } = "";
    public string SiteAddress { get; set; } = "";
    public string Area { get; set; } = "Wasserschadensanierung";
    public string WorkDescription { get; set; } = "";
    // Gespeicherte eindeutige Katalog-IDs (CSV); niemals UI-Indizes speichern.
    public string SelectedActivities { get; set; } = "";
    public string Responsible { get; set; } = "";
    public string Status { get; set; } = "Entwurf";
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedUtc { get; set; }
    public ObservableCollection<AssessmentHazard> Hazards { get; set; } = new();
    public ObservableCollection<SiteCheck> SiteChecks { get; set; } = new();
    public string DisplayName => string.IsNullOrWhiteSpace(ProjectNumber) ? "(ohne Projektnummer)" : ProjectNumber;
    public string UpdatedDisplay => UpdatedUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
}

public sealed class AssessmentHazard
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string SourceId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Measures { get; set; } = "";
    public int BeforeProbability { get; set; } = 2;
    public int BeforeSeverity { get; set; } = 2;
    public int AfterProbability { get; set; } = 1;
    public int AfterSeverity { get; set; } = 2;
    public string Owner { get; set; } = "";
    public DateTime? DueDate { get; set; }
    public bool EffectivenessChecked { get; set; }
    public int RiskBefore => BeforeProbability * BeforeSeverity;
    public int RiskAfter => AfterProbability * AfterSeverity;
}

public sealed record CatalogEntry(string Id, string Area, string Title, string Description, string SuggestedMeasures, string Reference)
{
    public override string ToString() => Title;
}

/// <summary>Objekt-Check je Beurteilung. "Geprüft" ist eine Dokumentation der Anwenderangabe, kein technischer Nachweis.</summary>
public sealed class SiteCheck
{
    public string Id { get; set; } = "";
    public string Question { get; set; } = "";
    public string Area { get; set; } = "Allgemein";
    public string RelatedSources { get; set; } = "";
    public bool RequiresExpertClarification { get; set; }
    public string Result { get; set; } = "Ungeklärt";
    public string Notes { get; set; } = "";
    public bool ExpertClarificationDocumented { get; set; }
}
