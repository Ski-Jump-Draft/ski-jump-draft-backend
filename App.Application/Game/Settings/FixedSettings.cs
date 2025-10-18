namespace App.Application.Game.Settings;

public class FixedSettings(Domain.Game.Settings settings) : IGameSettingsFactory
{
    public Task<Domain.Game.Settings> Create(Guid? matchmakingId)
    {
        return Task.FromResult(settings);
    }
}