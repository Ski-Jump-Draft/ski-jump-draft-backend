using App.Application.Abstractions;
using App.Application.CompetitionEngine;
using Microsoft.Extensions.Hosting;

namespace App.Infrastructure.PluginsRegistration;

public class PluginRegistrationService : IHostedService
{
    private readonly IEnumerable<ICompetitionEnginePlugin> _plugins;
    private readonly ICompetitionEnginePluginRepository _repo;

    public PluginRegistrationService(IEnumerable<ICompetitionEnginePlugin> plugins,
        ICompetitionEnginePluginRepository repo)
    {
        _plugins = plugins;
        _repo = repo;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        foreach (var plugin in _plugins)
            await _repo.Register(plugin);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}