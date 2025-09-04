namespace App.Application.Matchmaking;

public interface IMatchmakingDurationCalculator
{
    TimeSpan Calculate(App.Domain.Matchmaking.Matchmaking matchmaking);
}