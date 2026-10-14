using CM.Gefaehrdungsbeurteilung.Models;

namespace CM.Gefaehrdungsbeurteilung.Services;

// Orientierende Tätigkeitsauswahl; Vorschläge sind keine fertige Gefährdungsbeurteilung.
public sealed record ActivityDefinition(string Id, string Area, string Title, string[] SuggestedHazardIds);

public sealed class ActivityChoice
{
    public ActivityDefinition Definition { get; }
    public string Id => Definition.Id;
    public string Label => $"{Definition.Area}: {Definition.Title}";
    public bool IsSelected { get; set; }
    public ActivityChoice(ActivityDefinition definition, bool selected = false)
    {
        Definition = definition;
        IsSelected = selected;
    }
}

public static class ActivityService
{
    public static IReadOnlyList<ActivityDefinition> Entries { get; } = new List<ActivityDefinition>
    {
        new("W-LO", "Wasserschadensanierung", "Leckageortung und Feuchtemessung", new[] { "ALL-001", "ALL-002", "WAS-004" }),
        new("W-TR", "Wasserschadensanierung", "Technische Raum- und Bauteiltrocknung", new[] { "ALL-001", "ALL-002", "ALL-004", "WAS-003" }),
        new("W-DS", "Wasserschadensanierung", "Dämmschichttrocknung", new[] { "ALL-001", "ALL-003", "WAS-003", "WAS-004" }),
        new("W-RU", "Wasserschadensanierung", "Öffnung von Bauteilen und Rückbau", new[] { "ALL-003", "ALL-004", "ALL-005", "WAS-004" }),
        new("W-SC", "Wasserschadensanierung", "Schimmelpilzsanierung", new[] { "WAS-001", "ALL-003", "ALL-005" }),
        new("W-FK", "Wasserschadensanierung", "Fäkalwasser- / kontaminierter Wasserschaden", new[] { "WAS-002", "WAS-001", "ALL-002" }),
        new("B-BS", "Brandschadensanierung", "Besichtigung / Erstbewertung nach Brand", new[] { "BRA-001", "BRA-002", "BRA-003", "BRA-005" }),
        new("B-RS", "Brandschadensanierung", "Ruß- und Brandrückstandsreinigung", new[] { "BRA-001", "BRA-004", "ALL-003" }),
        new("B-RU", "Brandschadensanierung", "Kontaminierter Rückbau", new[] { "BRA-001", "BRA-002", "BRA-003", "BRA-004" }),
        new("B-DE", "Brandschadensanierung", "Dekontamination und Abfalltransport", new[] { "BRA-001", "BRA-004", "ALL-005" }),
        new("A-MA", "Allgemein", "Maschinen / Schleifen / Bohren", new[] { "ALL-003", "ALL-004", "ALL-001" }),
        new("A-LE", "Allgemein", "Leitern und Arbeiten in der Höhe", new[] { "ALL-006", "ALL-002" }),
        new("A-TR", "Allgemein", "Transport von Trocknungsgeräten / Material", new[] { "ALL-005", "ALL-002" })
    };

    public static HashSet<string> Ids(Assessment assessment) => assessment.SelectedActivities
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Where(id => Entries.Any(e => e.Id == id))
        .ToHashSet(StringComparer.Ordinal);

    public static IReadOnlyList<ActivityDefinition> Selected(Assessment assessment)
    {
        var ids = Ids(assessment);
        return Entries.Where(a => ids.Contains(a.Id)).ToArray();
    }

    public static HashSet<string> RelevantAreas(Assessment assessment)
    {
        var areas = Selected(assessment).Select(a => a.Area).ToHashSet(StringComparer.Ordinal);
        if (areas.Count == 0) areas.Add(assessment.Area);
        areas.Add("Allgemein");
        return areas;
    }

    public static List<CatalogEntry> Suggestions(Assessment assessment)
    {
        var sources = Selected(assessment).SelectMany(a => a.SuggestedHazardIds).ToHashSet(StringComparer.Ordinal);
        return CatalogService.Entries.Where(c => sources.Contains(c.Id) &&
            !assessment.Hazards.Any(h => h.SourceId == c.Id)).ToList();
    }
}
