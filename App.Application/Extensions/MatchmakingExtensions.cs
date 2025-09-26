using App.Domain.Matchmaking;

namespace App.Application.Extensions;

public static class MatchmakingExtensions
{
    public static string FormattedStatus(this Domain.Matchmaking.Status status)
    {
        return status switch
        {
            Status.Ended endedStatus => endedStatus.Result switch
            {
                { IsSucceeded: true } => "Ended Succeeded",
                { IsNotEnoughPlayers: true } => "Ended NotEnoughPlayers",
                _ => throw new ArgumentOutOfRangeException()
            },
            Status.Failed failedStatus => "Failed " + failedStatus,
            { IsRunning: true } => "Running",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static Domain.Matchmaking.Status CreateDomainStatus(string status)
    {
        return status switch
        {
            "Running" => Status.Running,
            "Ended Succeeded" => Status.NewEnded(MatchmakingResult.Succeeded),
            "Ended NotEnoughPlayers" => Status.NewEnded(MatchmakingResult.NotEnoughPlayers),
            var v when v.StartsWith("Failed ") => Status.NewFailed(v.Substring("Failed ".Length)),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}