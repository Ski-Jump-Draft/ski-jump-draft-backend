using System.Collections.Concurrent;
using System.Collections.Immutable;
using App.Application.Commanding;
using App.Application.Extensions;
using App.Application.Matchmaking;
using App.Application.Utility;
using App.Domain.Matchmaking;

namespace App.Web.HostedServices.InternalTest;

public class BotJoiner(
    IMatchmakings matchmakings,
    ICommandBus bus,
    IRandom random,
    IMyLogger log,
    IClock clock)
    : BackgroundService
{
    private static readonly List<string> MaleNames =
    [
        "Marek", "Jakub", "Jan", "Piotr", "Paweł", "Krzysztof", "Tomasz", "Adam", "Andrzej", "Michał",
        "Łukasz", "Mateusz", "Maciej", "Marcin", "Grzegorz", "Rafał", "Kamil", "Dawid", "Patryk", "Artur"
    ];

    private static readonly List<string> FemaleNames =
    [
        "Anna", "Maria", "Katarzyna", "Agnieszka", "Małgorzata", "Ewa", "Magdalena", "Joanna", "Monika",
        "Aleksandra", "Barbara", "Beata", "Natalia", "Karolina", "Dorota", "Sylwia", "Paulina", "Justyna",
        "Elżbieta", "Weronika"
    ];

    private static readonly List<string> AllNames = MaleNames.Concat(FemaleNames)
        .Select(n => $"Bot {n}").ToList();

    private readonly ConcurrentDictionary<Guid, ConcurrentBag<string>> _usedNicks = new();
    private readonly ConcurrentDictionary<Guid, bool> _botsJoined = new();

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var all = (await matchmakings.GetInProgress(ct)).ToImmutableArray();
            var now = clock.Now();

            var tasks = all
                .Where(m =>
                    MatchmakingIsEligibleForBots(m, now))
                .Select(m => JoinBotsToMatchmaking(m, ct));

            await Task.WhenAll(tasks);
            await Task.Delay(3000, ct);
            continue;

            bool MatchmakingIsEligibleForBots(Matchmaking m, DateTimeOffset nowDateTime)
            {
                return !_botsJoined.ContainsKey(m.Id_.Item) && m.RemainingSlots > 0
                                                            && m.RemainingToForceEnd(nowDateTime).TotalSeconds > 10;
            }
        }
    }

    private async Task JoinBotsToMatchmaking(Matchmaking m, CancellationToken ct)
    {
        var botsToJoin = Math.Max(1, m.RemainingSlots / 2);
        var success = false;

        var tasks = Enumerable.Range(0, botsToJoin).Select(async i =>
        {
            await Task.Delay(350 * i, ct);
            await JoinBotToMatchmaking(m.Id_.Item, ct);
            success = true;
        });

        await Task.WhenAll(tasks);
        if (success)
            _botsJoined[m.Id_.Item] = true;
    }


    private async Task JoinBotToMatchmaking(Guid matchmakingId, CancellationToken ct)
    {
        var nick = GenerateBotName(matchmakingId);
        var cmd = new App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.Command(nick, true);

        try
        {
            var (id, corrected, pid) =
                await bus.SendAsync<
                    App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.Command,
                    App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.Result>(cmd, ct);

            _usedNicks.GetOrAdd(id, _ => []).Add(corrected);
            log.Debug($"Bot {corrected} joined {id} (playerId={pid})");
        }
        catch (Exception ex) when (ex is
                                       App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.RoomIsFullException or
                                       App.Application.UseCase.Matchmaking.JoinQuickMatchmaking
                                           .MultipleGamesNotSupportedException)
        {
        }
        catch (Exception ex)
        {
            log.Error("Bot failed to join", ex);
        }
    }

    private string GenerateBotName(Guid matchmakingId)
    {
        var used = _usedNicks.GetOrAdd(matchmakingId, _ => new());
        var allowed = AllNames.Except(used).ToList();
        var chosen = allowed.Count == 0 ? "Bot" : allowed.GetRandomElement(random);
        used.Add(chosen);
        return chosen;
    }
}