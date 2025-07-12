using App.Application.UseCase.Competition.Engine.Factory;
using App.Domain.Competition;

namespace App.Application.Abstractions;

public interface ICompetitionEnginePlugin
{
    string PluginId { get; }
    Engine.ITemplate Template { get; }
    ICompetitionEngineFactory Factory { get; }
}

public interface ICompetitionEnginePluginRepository
{
    Task<IEnumerable<ICompetitionEnginePlugin>> GetPluginsAsync(CancellationToken cancellationToken = default);
    Task<ICompetitionEnginePlugin> GetByIdAsync(string id, CancellationToken cancellationToken = default);
}