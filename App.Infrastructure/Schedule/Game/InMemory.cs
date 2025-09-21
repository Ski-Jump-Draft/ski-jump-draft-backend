using System.Collections.Concurrent;
using App.Application.Game;
using App.Application.Utility;

namespace App.Infrastructure.Schedule.Game;

public sealed class InMemory(IClock clock, IMyLogger logger) : IGameSchedule
{
    private readonly ConcurrentDictionary<Guid, GameScheduleDto> _dtos = new();

    public void ScheduleEvent(Guid gameId, GameScheduleTarget scheduleTarget, TimeSpan @in)
    {
        var now = clock.Now();
        logger.Info($"now: {now}, scheduledAt: {now}, @in: {@in}, scheduleTarget: {scheduleTarget}, gameId: {
            gameId}");
        if (!CanSchedulePhaseFor(gameId, now)) return;
        var dto = new GameScheduleDto(gameId, scheduleTarget, @in, ScheduledAt: now);
        if (!CanBeScheduled(dto, now)) return;
        _dtos[gameId] = dto;
    }


    public bool Remove(Guid gameId) => _dtos.Remove(gameId, out _);

    private bool CanBeScheduled(GameScheduleDto dto, DateTimeOffset now)
    {
        return !dto.BreakPassed(now);
    }

    private bool CanSchedulePhaseFor(Guid gameId, DateTimeOffset now)
    {
        var existingDto = _dtos.GetValueOrDefault(gameId);
        var breakPassed = existingDto?.BreakPassed(now) ?? false;
        logger.Info($"existingDto: {existingDto}, breakPassed: {breakPassed}");
        return existingDto is null || breakPassed;
    }

    public GameScheduleDto? GetGameSchedule(Guid gameId)
    {
        return _dtos.GetValueOrDefault(gameId);
    }
}