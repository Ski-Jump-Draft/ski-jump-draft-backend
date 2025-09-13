using System.Collections.Concurrent;
using App.Application.Game;
using App.Application.Utility;

namespace App.Infrastructure.Schedule.Game;

public sealed class InMemory(IClock clock) : IGameSchedule
{
    private readonly ConcurrentDictionary<Guid, GameScheduleDto> _dtos = new();

    public void SchedulePhase(Guid gameId, GamePhase phase, TimeSpan @in)
    {
        var now = clock.Now();
        var scheduledAt = now + @in;
        var dto = new GameScheduleDto(gameId, phase, @in, scheduledAt);
        if (!CanSchedule(dto)) return;
        _dtos[gameId] = dto;
    }

    public bool Remove(Guid gameId) => _dtos.Remove(gameId, out _);

    private static bool CanSchedule(GameScheduleDto? dto)
    {
        return dto is null || dto.BreakPassed;
    }

    public GameScheduleDto? GetGameSchedule(Guid gameId)
    {
        return _dtos.GetValueOrDefault(gameId);
    }
}