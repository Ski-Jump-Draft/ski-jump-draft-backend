using System.Text.Json;
using App.Domain.Competition;

namespace App.Application.Abstractions;

public class CompetitionPluginMetadata
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
}

public sealed class EngineRawConfig
{
    public Guid Id { get; init; }
    public string Variant { get; init; } = string.Empty;
    public Dictionary<string, JsonElement> Options { get; init; } = new();
}

public interface IAdvancementTieBreaker
{
    /// Zwraca podzbiór `tied` (może być większy niż 1 gdy dopuszczasz wielu ex-aequo)
    IEnumerable<Guid> BreakTies(
        IEnumerable<Guid> tied);
}


