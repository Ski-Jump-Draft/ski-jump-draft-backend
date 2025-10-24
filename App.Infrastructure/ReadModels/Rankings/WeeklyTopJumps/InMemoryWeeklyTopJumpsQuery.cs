using App.Application.Acl;
using App.Application.Exceptions;
using App.Application.Game.DraftPicks;
using App.Application.Game.GameCompetitions;
using App.Application.UseCase.Rankings.WeeklyTopJumps;
using App.Application.Extensions;
using App.Application.Utility;
using App.Domain.Game;
using App.Domain.GameWorld;
using HillId = App.Domain.Game.HillId;

namespace App.Infrastructure.ReadModels.Rankings.WeeklyTopJumps;

/// <summary>
/// Dev/mock implementation that does not require Redis. It uses in-memory repositories/archives.
/// Works with mocked (non-prod) DI where IGames/Archives are in-memory.
/// </summary>
public class InMemoryWeeklyTopJumpsQuery(
    IGames games,
    IGameCompetitionResultsArchive competitionResultsArchive,
    IDraftPicksArchive draftPicksArchive,
    ICompetitionHillAcl competitionHillAcl,
    IHills hills,
    IJumpers jumpers,
    IMyLogger logger) : IWeeklyTopJumpsQuery
{
    public async Task<IReadOnlyList<WeeklyTopJumpDto>> GetTop20Last7Days(CancellationToken ct)
    {
        // Time window: last 7 days (dev env usually runs short-lived processes, so most data will match)
        var now = DateTimeOffset.UtcNow;
        var from = now.AddDays(-7);

        var endedGames = await games.GetEnded(ct);
        if (endedGames is null) return [];

        var list = new List<WeeklyTopJumpDto>(64);

        foreach (var game in endedGames)
        {
            if (ct.IsCancellationRequested) break;

            // There is no CreatedAt on domain Game; in dev/mock, treat all in-memory ended games as "now"
            var createdAt = now;

            if (createdAt < from) continue; // keep the same filtering contract

            var gameId = game.Id_.Item;

            var hillOpt = game.Hill;
            if (hillOpt.IsNone())
                continue;
            var hill = hillOpt.Value;

            // Pull archived main competition results (mocked archive provides them for dev)
            var archiveResults = await competitionResultsArchive.GetMainResultsAsync(gameId, ct);
            if (archiveResults is null) continue;

            // Prepare mapping GameJumperId -> Draft player nicks
            var draftPicks = await draftPicksArchive.GetPicks(gameId) ??
                             new Dictionary<App.Domain.Game.PlayerId, IEnumerable<App.Domain.Game.JumperId>>();
            // Build players map from domain game
            var players = App.Domain.Game.PlayersModule.toList(game.Players)
                .ToDictionary(p => p.Id.Item, p => App.Domain.Game.PlayerModule.NickModule.value(p.Nick));

            var nicksByGameJumperId = new Dictionary<Guid, List<string>>();
            foreach (var kv in draftPicks)
            {
                var playerGuid = kv.Key.Item;
                var nick = players.TryGetValue(playerGuid, out var n) ? n : null;
                if (nick is null) continue;

                foreach (var jumperId in kv.Value)
                {
                    var g = jumperId.Item;
                    if (!nicksByGameJumperId.TryGetValue(g, out var arr))
                    {
                        arr = new List<string>();
                        nicksByGameJumperId[g] = arr;
                    }

                    if (!arr.Contains(nick)) arr.Add(nick);
                }
            }

            var jumpersMeta = await LoadJumpersMetaAsync(nicksByGameJumperId.Keys, ct);

            foreach (var jumperResult in archiveResults.JumperResults)
            {
                var gameJumperId = jumperResult.GameJumperId;
                IReadOnlyList<string> draftNicks = nicksByGameJumperId.TryGetValue(gameJumperId, out var arr)
                    ? arr
                    : [];

                foreach (var jump in jumperResult.Jumps)
                {
                    var gwjId = jumperResult.GameWorldJumperId;

                    var (name, surname, country) = jumpersMeta.TryGetValue(gwjId, out var m)
                        ? m
                        : ("?", "?", "?");

                    var gameWorldHillId = competitionHillAcl.GetGameWorldHill(hill.Id.Item).Id;
                    var gameWorldHill = await hills.GetById(Domain.GameWorld.HillId.NewHillId(gameWorldHillId), ct)
                        .AwaitOrWrap(_ => new IdNotFoundException(gameWorldHillId));

                    list.Add(new WeeklyTopJumpDto(
                        gameId,
                        createdAt,
                        hill.Id.Item,
                        App.Domain.Competition.HillModule.KPointModule.value(hill.KPoint),
                        App.Domain.Competition.HillModule.HsPointModule.value(hill.HsPoint),
                        gameWorldHill.Location.Item,
                        CountryFisCodeModule.value(gameWorldHill.CountryCode),
                        jump.CompetitionJumperId,
                        gwjId,
                        name,
                        surname,
                        country,
                        jump.Distance,
                        jump.WindAverage,
                        jump.Gate,
                        draftNicks
                    ));
                }
            }
        }

        var top = list
            .OrderByDescending(r => r.Distance)
            .ThenBy(r => r.GameCreatedAt)
            .Take(20)
            .ToList();

        return top;
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
            logger.Warn("Jumpers meta load failed", ex, new { count = set.Count });
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