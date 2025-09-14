using App.Application.Utility;

namespace App.Application.Game;

public record GameScheduleDto(
    Guid GameId,
    GameScheduleTarget ScheduleTarget,
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
    void ScheduleEvent(Guid gameId, GameScheduleTarget scheduleTarget, TimeSpan @in);
    bool Remove(Guid gameId);
    GameScheduleDto? GetGameSchedule(Guid gameId);
}