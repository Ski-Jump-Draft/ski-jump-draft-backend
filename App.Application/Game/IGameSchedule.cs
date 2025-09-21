using App.Application.Utility;

namespace App.Application.Game;

public record GameScheduleDto(
    Guid GameId,
    GameScheduleTarget ScheduleTarget,
    TimeSpan In,
    DateTimeOffset ScheduledAt)
{
    public TimeSpan BreakRemaining(DateTimeOffset now) => In - (now - ScheduledAt);

    public bool BreakPassed(DateTimeOffset now)
    {
        return BreakRemaining(now) <= TimeSpan.Zero;
    }
};

public interface IGameSchedule
{
    void ScheduleEvent(Guid gameId, GameScheduleTarget scheduleTarget, TimeSpan @in);
    bool Remove(Guid gameId);
    GameScheduleDto? GetGameSchedule(Guid gameId);
}