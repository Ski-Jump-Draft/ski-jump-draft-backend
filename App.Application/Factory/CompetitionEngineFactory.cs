using App.Domain.Competition;

namespace App.Application.Factory;

public interface ICompetitionEngineFactory
{
    Domain.Competition.Engine.IEngine Create(Context context);
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

public sealed record Context(Guid EngineId, Dictionary<string, object> RawOptions, Hill Hill);