using App.Application.ReadModel.Projection;
using System.Text.Json;
using App.Application.Commanding;
using App.Domain.Game;
using App.Domain.Shared;
using StackExchange.Redis;
using GameModule = App.Domain.Game.GameModule;

namespace App.Infrastructure.Projection.Game.ActiveGames;

public class RedisActiveGamesProjection(IConnectionMultiplexer connectionMultiplexer) :
    IActiveGamesProjection,
    IEventHandler<Event.GameEventPayload>
{
    private readonly IDatabase _db = connectionMultiplexer.GetDatabase();

    private static readonly JsonSerializerOptions JsonOpts = new()
        { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private const string ActiveSetKey = "active-games";

    public async Task<IEnumerable<ActiveGameDto>> GetActiveGamesAsync(CancellationToken ct)
    {
        var keys = await _db.SetMembersAsync(ActiveSetKey);
        var list = new List<ActiveGameDto>(keys.Length);
        foreach (var keyVal in keys)
        {
            var key = (RedisKey)(string)keyVal!;
            var json = await _db.StringGetAsync(GameKey(key));
            if (json.IsNullOrEmpty) continue;
            var dto = JsonSerializer.Deserialize<ActiveGameDto>(json!, JsonOpts);
            if (dto is not null) list.Add(dto);
        }

        return list;
    }

    public async Task<ActiveGameDto?> GetByIdAsync(Id.Id gameId, CancellationToken ct)
    {
        var json = await _db.StringGetAsync(GameKey(gameId.Item));
        return json.IsNullOrEmpty
            ? null
            : JsonSerializer.Deserialize<ActiveGameDto>(json!, JsonOpts);
    }

    public async Task<ActiveGameTimeLimitsDto?> GetTimeLimitsByIdAsync(Id.Id gameId, CancellationToken ct)
    {
        var json = await _db.StringGetAsync(LimitsKey(gameId.Item));
        return json.IsNullOrEmpty
            ? null
            : JsonSerializer.Deserialize<ActiveGameTimeLimitsDto>(json!, JsonOpts);
    }

    public async Task HandleAsync(DomainEvent<Event.GameEventPayload> ev, CancellationToken ct)
    {
        var occurred = ev.Header.OccurredAt;

        switch (ev.Payload)
        {
            case Event.GameEventPayload.GameCreatedV1 p:
                var settings = p.Item.Settings;
                var preDraftDelay =
                    (settings.StartingPreDraftPolicy as Settings.PhaseTransitionPolicy.StartingPreDraft.AutoAfter)?.Item
                    ?.Value;
                var draftDelay =
                    (settings.StartingDraftPolicy as Settings.PhaseTransitionPolicy.StartingDraft.AutoAfter)?.Item
                    ?.Value;
                var compDelay =
                    (settings.StartingCompetitionPolicy as Settings.PhaseTransitionPolicy.StartingCompetition.AutoAfter)
                    ?.Item?.Value;
                var endingDelay = (settings.EndingGamePolicy as Settings.PhaseTransitionPolicy.EndingGame.AutoAfter)
                    ?.Item?.Value;

                var dto = new ActiveGameDto(
                    p.Item.GameId.Item,
                    MapPhase(GameModule.Phase.NewBreak(GameModule.PhaseTag.PreDraftTag)),
                    occurred);

                var limits = new ActiveGameTimeLimitsDto(
                    p.Item.GameId.Item,
                    BreakBeforePreDraft: preDraftDelay,
                    BreakBeforeDraft: draftDelay,
                    BreakBeforeCompetition: compDelay,
                    BreakBeforeEnding: endingDelay
                );

                await StoreAsync(dto, limits);
                break;

            case Event.GameEventPayload.PreDraftPhaseStartedV1 p:
                await UpdatePhaseAsync(p.Item.GameId, GamePhase.PreDraft);
                break;

            case Event.GameEventPayload.DraftPhaseStartedV1 p:
                await UpdatePhaseAsync(p.Item.GameId, GamePhase.Draft);
                break;

            case Event.GameEventPayload.CompetitionPhaseStartedV1 p:
                await UpdatePhaseAsync(p.Item.GameId, GamePhase.Competition);
                break;

            case Event.GameEventPayload.GameEndedV1 p:
                await RemoveAsync(p.Item.GameId.Item);
                break;
        }
    }

    private static string GameKey(Guid gameId) => $"game:{gameId}";
    private static string GameKey(RedisKey k) => k.ToString(); // Overload for usage in foreach
    private static string LimitsKey(Guid gameId) => $"game:limits:{gameId}";

    private async Task StoreAsync(ActiveGameDto dto, ActiveGameTimeLimitsDto limits)
    {
        var json = JsonSerializer.Serialize(dto, JsonOpts);
        var limitsJson = JsonSerializer.Serialize(limits, JsonOpts);
        await _db.StringSetAsync(GameKey(dto.GameId), json);
        await _db.StringSetAsync(LimitsKey(dto.GameId), limitsJson);
        await _db.SetAddAsync(ActiveSetKey, GameKey(dto.GameId));
    }

    private async Task UpdatePhaseAsync(Id.Id id, GamePhase phase)
    {
        var dto = await GetByIdAsync(id, CancellationToken.None);
        if (dto is null) return;
        var updated = dto with { Phase = phase };
        var json = JsonSerializer.Serialize(updated, JsonOpts);
        await _db.StringSetAsync(GameKey(id.Item), json);
    }

    private async Task RemoveAsync(Guid id)
    {
        await _db.KeyDeleteAsync(GameKey(id));
        await _db.KeyDeleteAsync(LimitsKey(id));
        await _db.SetRemoveAsync(ActiveSetKey, GameKey(id));
    }

    private static GamePhase MapPhase(GameModule.Phase ph) => ph switch
    {
        GameModule.Phase.PreDraft _ => GamePhase.PreDraft,
        GameModule.Phase.Draft _ => GamePhase.Draft,
        GameModule.Phase.Competition _ => GamePhase.Competition,
        GameModule.Phase.Ended _ => GamePhase.Ended,
        GameModule.Phase.Break _ => GamePhase.Break,
        _ => GamePhase.Break
    };
}