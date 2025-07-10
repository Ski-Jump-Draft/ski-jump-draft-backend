using App.Application.Competition.Engine.Creation;

namespace App.Application.Competition.Engine.Factory;

public interface ICompetitionEngineFactory
{
    Domain.Competition.Engine.IEngine Create(Engine.Creation.Context context);
    IEnumerable<Creation.Option> RequiredOptions { get; }
}