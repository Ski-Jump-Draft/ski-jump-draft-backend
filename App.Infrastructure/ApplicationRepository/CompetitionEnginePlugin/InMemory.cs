using System.Collections.Concurrent;
using App.Application.Abstractions;

namespace App.Infrastructure.ApplicationRepository.CompetitionEnginePlugin;

public class InMemory : ICompetitionEnginePluginRepository
{
    private readonly ConcurrentDictionary<string, ICompetitionEnginePlugin> _plugins = new();

    public Task Register(ICompetitionEnginePlugin plugin)
    {
        _plugins[plugin.PluginId] = plugin;
        return Task.CompletedTask;
    }

    public Task<ICompetitionEnginePlugin?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        _plugins.TryGetValue(id, out var plugin);
        return Task.FromResult(plugin);
    }
}