using App.Application.UseCase.Competition.Engine.Creation;

namespace App.Application.UseCase.Competition.Engine.Factory;

public interface ICompetitionEngineFactory
{
    Domain.Competition.Engine.IEngine Create(Engine.Creation.Context context);
    IEnumerable<Creation.Option> RequiredOptions { get; }
}