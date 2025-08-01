namespace App.Application.CompetitionEngine;

public interface ICompetitionEngineFactoryProvider
{
    ICompetitionEngineFactory Provide(EngineFactoryRequest request);
}

public sealed record EngineFactoryRequest(
    Domain.Competition.Engine.Metadata.Name EngineName,
    Domain.Competition.Engine.Metadata.Version EngineVersion
);