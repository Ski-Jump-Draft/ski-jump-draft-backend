using App.Application.UseCase.Helper;
using App.Domain.Matchmaking;

namespace App.Infrastructure.Globals;

public class DefaultQuickGameMatchmakingSettingsProvider : IQuickGameMatchmakingSettingsProvider
{
    public Task<Settings> Provide()
    {
        return Task.FromResult(new Settings(
            minParticipants: PlayersCountModule.tryCreate(1),
            maxParticipants: PlayersCountModule.tryCreate(10),
            maxDuration: Duration.NewDuration(TimeSpan.FromMinutes(2))));
    }
}