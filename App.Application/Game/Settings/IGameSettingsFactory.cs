namespace App.Application.Game.Settings;

public interface IGameSettingsFactory
{
    Task<Domain.Game.Settings> Create(Guid? matchmakingId);
}