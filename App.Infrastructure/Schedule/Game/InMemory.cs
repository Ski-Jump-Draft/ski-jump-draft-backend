using System.Collections.Concurrent;
using App.Application.Game;
using App.Application.Utility;

namespace App.Infrastructure.Schedule.Game;

public sealed class InMemory(IClock clock) : IGameSchedule
{
    private readonly ConcurrentDictionary<Guid, GameScheduleDto> _dtos = new();

    public void ScheduleEvent(Guid gameId, GameScheduleTarget scheduleTarget, TimeSpan @in)
    {
        var now = clock.Now();
        var scheduledAt = now + @in;
        if (!CanSchedulePhaseFor(gameId)) return;
        var dto = new GameScheduleDto(gameId, scheduleTarget, @in, scheduledAt);
        if (!CanBeScheduled(dto)) return;
        _dtos[gameId] = dto;
    }


    public bool Remove(Guid gameId) => _dtos.Remove(gameId, out _);

    private bool CanBeScheduled(GameScheduleDto dto)
    {
        return !dto.BreakPassed(clock);
    }

    private bool CanSchedulePhaseFor(Guid gameId)
    {
        var existingDto = _dtos.GetValueOrDefault(gameId);
        return existingDto is null || !existingDto.BreakPassed(clock);
    }

    public GameScheduleDto? GetGameSchedule(Guid gameId)
    {
        return _dtos.GetValueOrDefault(gameId);
    }
}