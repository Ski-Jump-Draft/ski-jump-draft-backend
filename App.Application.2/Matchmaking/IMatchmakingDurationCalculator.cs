namespace App.Application._2.Matchmaking;

public interface IMatchmakingDurationCalculator
{
    TimeSpan Calculate(App.Domain._2.Matchmaking.Matchmaking matchmaking);
}