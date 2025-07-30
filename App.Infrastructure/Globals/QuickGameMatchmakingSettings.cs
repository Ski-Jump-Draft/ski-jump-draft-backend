using App.Application.UseCase.Helper;
using App.Domain.Matchmaking;

namespace App.Infrastructure.Globals;

public class DefaultQuickGameMatchmakingSettingsProvider : IQuickGameMatchmakingSettingsProvider
{
    public Task<Settings> Provide()
    {
        return Task.FromResult(new Settings(
            PlayersCountModule.tryCreate(1).ResultValue,
            PlayersCountModule.tryCreate(10).ResultValue));
    }
}