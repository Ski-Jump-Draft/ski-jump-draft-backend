using App.Domain.Competition;

namespace App.Application.CompetitionEngine;

public interface ICompetitionEngineFactory
{
    Engine.IEngine Create(CreationContext context);
    IEnumerable<Option> RequiredOptions { get; }
}

public enum OptionType
{
    String,
    Double,
    Boolean,
    ListOfIntegers,
    ListOfStrings
}

public sealed record Option(string Key, OptionType Type);

public sealed record CreationContext(
    Dictionary<string, object> RawOptions,
    Hill Hill,
    object RandomSeed);