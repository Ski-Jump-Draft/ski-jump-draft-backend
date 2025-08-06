using App.Application.ReadModel.Projection;

namespace App.Infrastructure.Projection.Game.ActiveGames;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Application.Commanding;
using App.Domain.Game;
using App.Domain.Shared;
using StackExchange.Redis;
using GameModule = App.Domain.Game.GameModule;

public class RedisActiveGamesProjection(IConnectionMultiplexer connectionMultiplexer) :
    IActiveGamesProjection,
    IEventHandler<Event.GameEventPayload>
{
    private readonly IDatabase _db = connectionMultiplexer.GetDatabase();
    private static readonly JsonSerializerOptions JsonOpts = new()
        { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private const string ActiveSetKey = "active-games";

    /* ---------- odczyt (query side) ---------- */

    public async Task<IEnumerable<ActiveGameDto>> GetActiveGamesAsync(CancellationToken ct)
    {
        var keys = await _db.SetMembersAsync(ActiveSetKey);
        var list = new List<ActiveGameDto>(keys.Length);

        foreach (var keyVal in keys)
        {
            var key = (RedisKey)(string)keyVal!; // rzutowanie RedisValue → string → RedisKey
            var json = await _db.StringGetAsync(key);
            if (json.IsNullOrEmpty) continue;
            var dto = JsonSerializer.Deserialize<ActiveGameDto>(json!, JsonOpts);
            if (dto is not null) list.Add(dto);
        }

        return list;
    }

    public async Task<ActiveGameDto?> GetByIdAsync(Guid gameId, CancellationToken ct)
    {
        var json = await _db.StringGetAsync(Key(gameId));
        return json.IsNullOrEmpty
            ? null
            : JsonSerializer.Deserialize<ActiveGameDto>(json!, JsonOpts);
    }


    public async Task HandleAsync(DomainEvent<Event.GameEventPayload> ev, CancellationToken ct)
    {
        var occurred = ev.Header.OccurredAt;

        switch (ev.Payload)
        {
            case Event.GameEventPayload.GameCreatedV1 p:
                var dto = new ActiveGameDto(
                    p.Item.GameId.Item,
                    MapPhase(GameModule.Phase.NewBreak(GameModule.PhaseTag.PreDraftTag)),
                    occurred);
                await StoreAsync(dto);
                break;

            case Event.GameEventPayload.PreDraftPhaseStartedV1 p:
                await UpdatePhaseAsync(p.Item.GameId.Item, GamePhase.PreDraft);
                break;

            case Event.GameEventPayload.DraftPhaseStartedV1 p:
                await UpdatePhaseAsync(p.Item.GameId.Item, GamePhase.Draft);
                break;

            case Event.GameEventPayload.CompetitionPhaseStartedV1 p:
                await UpdatePhaseAsync(p.Item.GameId.Item, GamePhase.Competition);
                break;

            case Event.GameEventPayload.GameEndedV1 p:
                await RemoveAsync(p.Item.GameId.Item);
                break;
        }
    }

    /* ---------- helpers ---------- */

    private static string Key(Guid gameId) => $"game:{gameId}";

    private async Task StoreAsync(ActiveGameDto dto)
    {
        var json = JsonSerializer.Serialize(dto, JsonOpts);
        await _db.StringSetAsync(Key(dto.GameId), json);
        await _db.SetAddAsync(ActiveSetKey, Key(dto.GameId));
    }

    private async Task UpdatePhaseAsync(Guid id, GamePhase phase)
    {
        var dto = await GetByIdAsync(id, CancellationToken.None);
        if (dto is null) return;

        var updated = dto with { Phase = phase };
        await StoreAsync(updated);
    }

    private async Task RemoveAsync(Guid id)
    {
        await _db.KeyDeleteAsync(Key(id));
        await _db.SetRemoveAsync(ActiveSetKey, Key(id));
    }

    private static GamePhase MapPhase(GameModule.Phase ph) => ph switch
    {
        GameModule.Phase.PreDraft _      => GamePhase.PreDraft,
        GameModule.Phase.Draft _         => GamePhase.Draft,
        GameModule.Phase.Competition _   => GamePhase.Competition,
        GameModule.Phase.Ended _         => GamePhase.Ended,
        GameModule.Phase.Break _         => GamePhase.Break,
        _                                => GamePhase.Break
    };
}
