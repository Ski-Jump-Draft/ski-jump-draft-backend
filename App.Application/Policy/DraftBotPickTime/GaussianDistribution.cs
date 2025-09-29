using App.Application.Utility;
using App.Domain.Game;

namespace App.Application.Policy.DraftBotPickTime;

public class GaussianDistribution(IRandom random) : IDraftBotPickTime
{
    public TimeSpan Get(DraftModule.SettingsModule.TimeoutPolicy timeoutPolicy)
    {
        var timeoutInSeconds = timeoutPolicy.ToSeconds();
        if (timeoutInSeconds is null)
        {
            return TimeSpan.Zero;
        }

        var mean = timeoutInSeconds.Value / 2.0;
        var stdDev = timeoutInSeconds.Value / 6.5;
        var randomSeconds = Math.Clamp(random.Gaussian(mean, stdDev), 0, timeoutInSeconds.Value);
        return TimeSpan.FromSeconds(randomSeconds);
    }
}