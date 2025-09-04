namespace App.Application.Matchmaking;

public class FixedMatchmakingDurationCalculator(TimeSpan duration) : IMatchmakingDurationCalculator
{
    public TimeSpan Calculate(Domain.Matchmaking.Matchmaking matchmaking)
    {
        return duration;
    }
}