namespace App.Application._2.Matchmaking;

public class FixedMatchmakingDurationCalculator(TimeSpan duration) : IMatchmakingDurationCalculator
{
    public TimeSpan Calculate(Domain._2.Matchmaking.Matchmaking matchmaking)
    {
        return duration;
    }
}