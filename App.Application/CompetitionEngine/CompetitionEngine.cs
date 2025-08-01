using App.Application.CompetitionEngine;
using App.Domain.Competition;

namespace App.Application.CompetitionEngine;

public interface ICompetitionEnginePlugin
{
    string PluginId { get; }
    Engine.IMetadata Metadata { get; }
    ICompetitionEngineFactory Factory { get; }
}

public interface ICompetitionEnginePluginRepository
{
    // TODO: Do read-modelu
    //
    //  Task<IEnumerable<ICompetitionEnginePlugin>> GetPluginsAsync(CancellationToken cancellationToken = default);

    Task Register(ICompetitionEnginePlugin plugin);

    Task<ICompetitionEnginePlugin?> GetByIdAsync(string pluginId,
        CancellationToken cancellationToken = default);
}