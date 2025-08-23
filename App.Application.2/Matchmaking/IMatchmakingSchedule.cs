namespace App.Application._2.Matchmaking;

public interface IMatchmakingSchedule
{
    void StartMatchmaking(Guid matchmakingId, TimeSpan maxDuration);
    void EndMatchmaking(Guid matchmakingId);
    TimeSpan GetRemainingTime(Guid matchmakingId);
    bool ShouldEnd(Guid matchmakingId);
}