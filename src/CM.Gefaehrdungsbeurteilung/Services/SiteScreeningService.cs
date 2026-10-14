using CM.Gefaehrdungsbeurteilung.Models;

namespace CM.Gefaehrdungsbeurteilung.Services;

/// <summary>
/// Orientierung für den Projektstart; nicht abschließend, nicht als fachlich freigegebener Katalog geeignet.
/// Neue Checks dürfen alte Bewertungen nicht unbemerkt ändern: sie werden als ungeklärt ergänzt.
/// </summary>
public static class SiteScreeningService
{
    public static IReadOnlyList<SiteCheck> Defaults { get; } = new List<SiteCheck>
    {
        new() { Id = "OBJ-EL", Area = "Allgemein", Question = "Nasse, beschädigte oder brandgeschädigte Elektroinstallation / elektrische Gefährdung?", RelatedSources = "ALL-001,BRA-005", RequiresExpertClarification = true },
        new() { Id = "OBJ-AS", Area = "Allgemein", Question = "Verdacht auf Asbest, alte Mineralwolle oder weitere Gefahrstoffe in Bestandsbaustoffen?", RelatedSources = "ALL-003,BRA-002", RequiresExpertClarification = true },
        new() { Id = "OBJ-ST", Area = "Allgemein", Question = "Instabile oder beschädigte Bauteile; unklare Standsicherheit?", RelatedSources = "ALL-007,BRA-003", RequiresExpertClarification = true },
        new() { Id = "OBJ-MI", Area = "Wasserschadensanierung", Question = "Schimmel, mikrobiologische Belastung oder fäkalienhaltiges Wasser vorhanden?", RelatedSources = "WAS-001,WAS-002", RequiresExpertClarification = false },
        new() { Id = "OBJ-TK", Area = "Wasserschadensanierung", Question = "Kontaminationsverdacht in Dämmschichten oder bei der Luftführung der Trocknung?", RelatedSources = "WAS-003", RequiresExpertClarification = false },
        new() { Id = "OBJ-RU", Area = "Brandschadensanierung", Question = "Ruß, Brandrückstände oder andere brandbedingte Kontamination vorhanden?", RelatedSources = "BRA-001", RequiresExpertClarification = false }
    };

    public static void EnsureDefaults(Assessment assessment)
    {
        foreach (var d in Defaults)
        {
            if (assessment.SiteChecks.Any(c => c.Id == d.Id)) continue;
            assessment.SiteChecks.Add(new SiteCheck
            {
                Id = d.Id, Question = d.Question, Area = d.Area,
                RelatedSources = d.RelatedSources, RequiresExpertClarification = d.RequiresExpertClarification
            });
        }
    }

    public static List<SiteCheck> Applicable(Assessment assessment)
    {
        EnsureDefaults(assessment);
        var areas = ActivityService.RelevantAreas(assessment);
        return assessment.SiteChecks.Where(c => areas.Contains(c.Area)).ToList();
    }

    public static List<string> BlockingReasons(Assessment assessment)
    {
        var reasons = new List<string>();
        foreach (var c in Applicable(assessment))
        {
            if (c.Result != "Ja" && c.Result != "Nein")
            {
                reasons.Add($"Objekt-Check offen: {c.Question}");
                continue;
            }
            if (c.Result == "Nein") continue;
            if (string.IsNullOrWhiteSpace(c.Notes))
                reasons.Add($"Bei 'Ja' sind Befund und Umgang zu beschreiben: {c.Question}");
            var allowed = c.RelatedSources.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (!assessment.Hazards.Any(h => allowed.Contains(h.SourceId)))
                reasons.Add($"Zugehörige Gefährdung und Maßnahmen aufnehmen: {c.Question}");
            if (c.RequiresExpertClarification && !c.ExpertClarificationDocumented)
                reasons.Add($"Fachkundige Klärung/Nachweis noch nicht dokumentiert: {c.Question}");
        }
        return reasons;
    }
}
