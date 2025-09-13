namespace App.Application.Game;

public record GameScheduleDto(
    Guid GameId,
    GamePhase Phase,
    TimeSpan In,
    DateTimeOffset ScheduledAt)
{
    public bool BreakPassed => In.TotalMilliseconds <= 0;
};

public interface IGameSchedule
{
    void SchedulePhase(Guid gameId, GamePhase phase, TimeSpan @in);
    bool Remove(Guid gameId);
    GameScheduleDto? GetGameSchedule(Guid gameId);
}