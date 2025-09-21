using App.Domain.Game;

namespace App.Application.Policy.DraftBotPickTime;

public interface IDraftBotPickTime
{
    TimeSpan Get(DraftModule.SettingsModule.TimeoutPolicy timeoutPolicy);
}