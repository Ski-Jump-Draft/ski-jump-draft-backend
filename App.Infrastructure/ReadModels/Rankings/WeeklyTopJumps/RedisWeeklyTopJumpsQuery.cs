using System.Diagnostics;
using System.Text.Json;
using App.Application.Acl;
using App.Application.Exceptions;
using App.Application.Extensions;
using App.Application.Game.DraftPicks;
using App.Application.Game.GameCompetitions;
using App.Application.UseCase.Rankings.WeeklyTopJumps;
using StackExchange.Redis;
using RedisGameRepo = App.Infrastructure.Repository.Game;
using App.Application.Utility;
using App.Domain.GameWorld;

namespace App.Infrastructure.ReadModels.Rankings.WeeklyTopJumps;

public class RedisWeeklyTopJumpsQuery(
    IConnectionMultiplexer redis,
    IGameCompetitionResultsArchive competitionResultsArchive,
    IDraftPicksArchive draftPicksArchive,
    App.Domain.GameWorld.IJumpers jumpers,
    IMyLogger log,
    ICompetitionHillAcl competitionHillAcl,
    IHills hills) : IWeeklyTopJumpsQuery
{
    private readonly IDatabase _db = redis.GetDatabase();
    private const string ArchiveSetKey = "game:archive:ids";
    private static string ArchiveKey(Guid id) => $"game:archive:{id}";

    public async Task<IReadOnlyList<WeeklyTopJumpDto>> GetTop20Last7Days(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var from = now.AddDays(-7);
        var results = new List<WeeklyTopJumpDto>(128);

        var sw = Stopwatch.StartNew();
        const int timeBudgetMs = 3500;

        log.Info("WeeklyTopJumps start", new { from, now });

        var batch = new List<RedisValue>(100);
        long totalIds = 0, processed = 0;

        try
        {
            foreach (var idVal in _db.SetScan(ArchiveSetKey, pageSize: 100))
            {
                if (ct.IsCancellationRequested) break;
                batch.Add(idVal);
                totalIds++;

                if (batch.Count >= 100)
                {
                    await ProcessBatchAsync(batch, from, results, ct);
                    processed += batch.Count;
                    log.Debug("Batch processed", new { processed, elapsed = sw.ElapsedMilliseconds });
                    batch.Clear();
                    if (sw.ElapsedMilliseconds > timeBudgetMs) break;
                }
            }

            if (!ct.IsCancellationRequested && batch.Count > 0 && sw.ElapsedMilliseconds <= timeBudgetMs)
            {
                await ProcessBatchAsync(batch, from, results, ct);
                processed += batch.Count;
            }
        }
        catch (RedisTimeoutException ex)
        {
            log.Warn("Redis timeout scanning archives", ex);
        }

        log.Info("Processing done",
            new { totalIds, processed, collected = results.Count, elapsed = sw.ElapsedMilliseconds });

        var top = results
            .OrderByDescending(r => r.Distance)
            .ThenBy(r => r.GameCreatedAt)
            .Take(20)
            .ToList();

        log.Info("Top20 ready", new { count = top.Count });
        return top;

        async Task ProcessBatchAsync(List<RedisValue> ids, DateTimeOffset fromDate, List<WeeklyTopJumpDto> acc,
            CancellationToken token)
        {
            if (ids.Count == 0) return;

            var keyList = new List<RedisKey>(ids.Count);
            foreach (var id in ids)
                if (Guid.TryParse(id.ToString(), out var guid))
                    keyList.Add((RedisKey)ArchiveKey(guid));

            if (keyList.Count == 0) return;

            RedisValue[] values;
            try
            {
                values = await _db.StringGetAsync(keyList.ToArray());
            }
            catch (RedisTimeoutException ex)
            {
                log.Warn("Redis timeout on MGET", ex, new { count = keyList.Count });
                return;
            }

            foreach (var redisValue in values)
            {
                if (token.IsCancellationRequested) break;
                var json = redisValue;
                if (!json.HasValue) continue;

                RedisGameRepo.GameDto? gameDto = null;
                try
                {
                    gameDto = JsonSerializer.Deserialize<RedisGameRepo.GameDto>(json!);
                }
                catch (Exception ex)
                {
                    log.Warn("JSON malformed", ex);
                    continue;
                }

                if (gameDto is null) continue;
                var effectiveDate = gameDto.EndedAt ?? gameDto.CreatedAt;
                if (effectiveDate < fromDate) continue;
                var gameId = gameDto.Id;

                // Prefer using data present in archived GameDto (EndedMainCompetition) to avoid races and extra Redis calls.
                // Fallback to archive interface only if missing.
                Dictionary<App.Domain.Game.PlayerId, IEnumerable<App.Domain.Game.JumperId>>? draftPicks = null;
                try
                {
                    draftPicks = await draftPicksArchive.GetPicks(gameId);
                }
                catch (RedisTimeoutException ex)
                {
                    log.Warn("DraftPicks timeout", ex, new { gameId });
                }

                draftPicks ??= new();

                var playersById = gameDto.Players.ToDictionary(p => p.Id, p => p.Nick);
                var nicksByGameJumperId = new Dictionary<Guid, List<string>>();

                foreach (var kv in draftPicks)
                {
                    if (!playersById.TryGetValue(kv.Key.Item, out var nick) || nick is null) continue;
                    foreach (var jumperId in kv.Value)
                        nicksByGameJumperId.GetOrAdd(jumperId.Item, _ => new()).Add(nick);
                }

                var hill = gameDto.CompetitionHillDto;

                if (gameDto.EndedMainCompetition is { } ended)
                {
                    // If some IDs are missing (Guid.Empty), try to enrich from archive (has ACL fallback)
                    Dictionary<Guid, (Guid Gj, Guid Gwj)>? competitionJumperToOtherIds = null;
                    if (ended.Results.Any(r => r.GameWorldJumperId == Guid.Empty || r.GameJumperId == Guid.Empty))
                    {
                        try
                        {
                            var enriched = await competitionResultsArchive.GetMainResultsAsync(gameId, token);
                            if (enriched is not null)
                                competitionJumperToOtherIds = enriched.JumperResults.ToDictionary(
                                    x => x.CompetitionJumperId,
                                    x => (x.GameJumperId, x.GameWorldJumperId));
                        }
                        catch (RedisTimeoutException ex)
                        {
                            log.Warn("CompetitionResults timeout (enrich)", ex, new { gameId });
                        }
                        catch (Exception ex)
                        {
                            log.Warn("CompetitionResults enrich failed", ex, new { gameId });
                        }
                    }

                    // 4) Finally, try pre-draft archive (all ended pre-draft competitions)
                    if (ended.Results.Any(r => r.GameWorldJumperId == Guid.Empty || r.GameJumperId == Guid.Empty))
                    {
                        try
                        {
                            var pre = await competitionResultsArchive.GetPreDraftResultsAsync(gameId, token);
                            if (pre is not null)
                            {
                                foreach (var comp in pre)
                                {
                                    foreach (var x in comp.JumperResults)
                                    {
                                        if (x.GameWorldJumperId == Guid.Empty || x.GameJumperId == Guid.Empty) continue;
                                        if (!competitionJumperToOtherIds.ContainsKey(x.CompetitionJumperId))
                                            competitionJumperToOtherIds[x.CompetitionJumperId] =
                                                (x.GameJumperId, x.GameWorldJumperId);
                                    }
                                }
                            }
                        }
                        catch (RedisTimeoutException ex)
                        {
                            log.Warn("PreDraftResults timeout (enrich)", ex, new { gameId });
                        }
                        catch (Exception ex)
                        {
                            log.Warn("PreDraftResults enrich failed", ex, new { gameId });
                        }
                    }

                    // Collect effective GWJ ids
                    var gwjIds = new HashSet<Guid>();
                    foreach (var compRes in ended.Results)
                    {
                        var gwjIdEff = compRes.GameWorldJumperId != Guid.Empty
                            ? compRes.GameWorldJumperId
                            : (competitionJumperToOtherIds.TryGetValue(compRes.CompetitionJumperId, out var pair)
                                ? pair.Gwj
                                : Guid.Empty);
                        if (gwjIdEff != Guid.Empty) gwjIds.Add(gwjIdEff);
                    }

                    var gwMeta = await LoadJumpersMetaAsync(gwjIds, token);

                    foreach (var compRes in ended.Results)
                    {
                        var gjIdEff = compRes.GameJumperId != Guid.Empty
                            ? compRes.GameJumperId
                            : (competitionJumperToOtherIds is not null &&
                               competitionJumperToOtherIds.TryGetValue(compRes.CompetitionJumperId, out var pair)
                                ? pair.Gj
                                : Guid.Empty);
                        var gwjIdEff = compRes.GameWorldJumperId != Guid.Empty
                            ? compRes.GameWorldJumperId
                            : (competitionJumperToOtherIds is not null &&
                               competitionJumperToOtherIds.TryGetValue(compRes.CompetitionJumperId, out var pair2)
                                ? pair2.Gwj
                                : Guid.Empty);
                        
                        var gameWorldHillId = gameDto.CompetitionHillDto.GameWorldHillId;
                        
                        if (gjIdEff == Guid.Empty || gwjIdEff == Guid.Empty || gameWorldHillId == Guid.Empty)
                        {
                            continue;
                        }
                        

                        var (name, surname, country) = gwMeta.TryGetValue(gwjIdEff, out var m)
                            ? m
                            : ("?", "?", "?");
                        
                        
                        var gameWorldHill = await hills.GetById(HillId.NewHillId(gameWorldHillId), token)
                            .AwaitOrWrap(_ => new IdNotFoundException(gameWorldHillId));

                        acc.AddRange(compRes.RoundResults.Select(roundResultDto => new WeeklyTopJumpDto(gameId,
                            gameDto.CreatedAt, hill.Id, hill.KPoint, hill.HsPoint, gameWorldHill.Location.Item,
                            CountryFisCodeModule.value(gameWorldHill.CountryCode), roundResultDto.CompetitionJumperId,
                            gwjIdEff, name, surname, country, roundResultDto.Distance, roundResultDto.WindAverage,
                            roundResultDto.Gate,
                            (gjIdEff != Guid.Empty && nicksByGameJumperId.TryGetValue(gjIdEff, out var arr))
                                ? arr
                                : Array.Empty<string>())));
                    }
                }
                else
                {
                    ArchiveCompetitionResultsDto? archiveResults = null;
                    try
                    {
                        archiveResults = await competitionResultsArchive.GetMainResultsAsync(gameId, token);
                    }
                    catch (RedisTimeoutException ex)
                    {
                        log.Warn("CompetitionResults timeout", ex, new { gameId });
                    }

                    if (archiveResults is null) continue;

                    // Build CJ -> (GJ, GWJ) map: seed from main archive, then add from dto.PreDraft, then archived pre-draft
                    var cjToIds = new Dictionary<Guid, (Guid Gj, Guid Gwj)>();
                    foreach (var jr in archiveResults.JumperResults)
                    {
                        if (jr.GameWorldJumperId != Guid.Empty && jr.GameJumperId != Guid.Empty)
                            cjToIds[jr.CompetitionJumperId] = (jr.GameJumperId, jr.GameWorldJumperId);
                    }

                    if (gameDto.PreDraft?.EndedCompetitions is { } predraftEnded2 && predraftEnded2.Count > 0)
                    {
                        foreach (var comp in predraftEnded2)
                        {
                            foreach (var res in comp.Results)
                            {
                                if (res.GameWorldJumperId == Guid.Empty || res.GameJumperId == Guid.Empty) continue;
                                if (!cjToIds.ContainsKey(res.CompetitionJumperId))
                                    cjToIds[res.CompetitionJumperId] = (res.GameJumperId, res.GameWorldJumperId);
                            }
                        }
                    }

                    if (archiveResults.JumperResults.Any(r =>
                            r.GameWorldJumperId == Guid.Empty || r.GameJumperId == Guid.Empty))
                    {
                        try
                        {
                            var pre = await competitionResultsArchive.GetPreDraftResultsAsync(gameId, token);
                            if (pre is not null)
                            {
                                foreach (var comp in pre)
                                {
                                    foreach (var x in comp.JumperResults)
                                    {
                                        if (x.GameWorldJumperId == Guid.Empty || x.GameJumperId == Guid.Empty) continue;
                                        if (!cjToIds.ContainsKey(x.CompetitionJumperId))
                                            cjToIds[x.CompetitionJumperId] = (x.GameJumperId, x.GameWorldJumperId);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            log.Warn("PreDraftResults enrich (fallback) failed", ex, new { gameId });
                        }
                    }

                    // Meta needs effective GWJ ids
                    var gwjIds = new HashSet<Guid>();
                    foreach (var jr in archiveResults.JumperResults)
                    {
                        var gwjEff = jr.GameWorldJumperId != Guid.Empty
                            ? jr.GameWorldJumperId
                            : (cjToIds.TryGetValue(jr.CompetitionJumperId, out var p)
                                ? p.Gwj
                                : Guid.Empty);
                        if (gwjEff != Guid.Empty) gwjIds.Add(gwjEff);
                    }

                    var gwMeta = await LoadJumpersMetaAsync(gwjIds, token);

                    foreach (var jumperResult in archiveResults.JumperResults)
                    {
                        var gjEff = jumperResult.GameJumperId != Guid.Empty
                            ? jumperResult.GameJumperId
                            : (cjToIds.TryGetValue(jumperResult.CompetitionJumperId, out var p)
                                ? p.Gj
                                : Guid.Empty);
                        var gwjEff = jumperResult.GameWorldJumperId != Guid.Empty
                            ? jumperResult.GameWorldJumperId
                            : (cjToIds.TryGetValue(jumperResult.CompetitionJumperId, out var p2)
                                ? p2.Gwj
                                : Guid.Empty);

                        var (name, surname, country) = gwMeta.TryGetValue(gwjEff, out var m)
                            ? m
                            : ("?", "?", "?");

                        var gameWorldHillId = gameDto.CompetitionHillDto.GameWorldHillId;
                        var gameWorldHill = await hills.GetById(HillId.NewHillId(gameWorldHillId), token)
                            .AwaitOrWrap(_ => new IdNotFoundException(gameWorldHillId));

                        foreach (var jump in jumperResult.Jumps)
                        {
                            acc.Add(new WeeklyTopJumpDto(
                                gameId, gameDto.CreatedAt, hill.Id, hill.KPoint, hill.HsPoint,
                                gameWorldHill.Location.Item,
                                CountryFisCodeModule.value(gameWorldHill.CountryCode),
                                jump.CompetitionJumperId, gwjEff,
                                name, surname, country,
                                jump.Distance, jump.WindAverage, jump.Gate,
                                (gjEff != Guid.Empty && nicksByGameJumperId.TryGetValue(gjEff, out var arr)) ? arr : []
                            ));
                        }
                    }
                }
            }
        }

        async Task<Dictionary<Guid, (string Name, string Surname, string Country)>> LoadJumpersMetaAsync(
            IEnumerable<Guid> ids, CancellationToken token)
        {
            var set = ids?.Where(id => id != Guid.Empty).ToHashSet() ?? [];
            var dict = new Dictionary<Guid, (string, string, string)>(set.Count);
            if (set.Count == 0) return dict;

            var domainIds = set.Select(App.Domain.GameWorld.JumperId.NewJumperId);
            IEnumerable<App.Domain.GameWorld.Jumper> jw;
            try
            {
                jw = await jumpers.GetFromIds(domainIds, token);
            }
            catch (Exception ex)
            {
                log.Warn("Jumpers meta load failed", ex, new { count = set.Count });
                return dict;
            }

            foreach (var j in jw)
            {
                var code = App.Domain.GameWorld.CountryFisCodeModule.value(j.FisCountryCode);
                dict[j.Id.Item] = (j.Name.Item, j.Surname.Item, code);
            }

            return dict;
        }
    }
}

static class DictExt
{
    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key,
        Func<TKey, TValue> factory)
        where TKey : notnull
    {
        if (!dict.TryGetValue(key, out var val))
            dict[key] = val = factory(key);
        return val;
    }
}