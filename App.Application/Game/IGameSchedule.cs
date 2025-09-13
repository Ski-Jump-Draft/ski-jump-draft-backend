using App.Application.Utility;

namespace App.Application.Game;

public record GameScheduleDto(
    Guid GameId,
    GamePhase Phase,
    TimeSpan In,
    DateTimeOffset ScheduledAt)
{
    public TimeSpan BreakRemaining(IClock clock) => In - (clock.Now() - ScheduledAt);
    
    public bool BreakPassed(IClock clock)
    {
        return BreakRemaining(clock) <= TimeSpan.Zero;
    }
};

public interface IGameSchedule
{
    void SchedulePhase(Guid gameId, GamePhase phase, TimeSpan @in);
    bool Remove(Guid gameId);
    GameScheduleDto? GetGameSchedule(Guid gameId);
}