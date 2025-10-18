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
    IClock clock,
    IPremiumMatchmakingGames premiumMatchmakingGames)
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
            var all = (await matchmakings.GetInProgress(null, ct)).ToImmutableArray();
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
                var botsHaveNotJoined = !_botsJoined.ContainsKey(m.Id_.Item);
                var remainingSlotsExist = m.RemainingSlots > 0;
                var remainingTimeIsEnough = m.RemainingToForceEnd(nowDateTime).TotalSeconds > 10;
                return botsHaveNotJoined && remainingSlotsExist
                                         && remainingTimeIsEnough;
            }
        }
    }

    private async Task JoinBotsToMatchmaking(Matchmaking m, CancellationToken ct)
    {
        var botsToJoin = Math.Max(1, (int)Math.Ceiling(m.RemainingSlots / 1.5));
        var success = false;

        var tasks = Enumerable.Range(0, botsToJoin).Select(async i =>
        {
            await Task.Delay(350 * i, ct);
            await JoinBotToMatchmaking(m.Id_.Item, m.IsPremium_, ct);
            success = true;
        });

        await Task.WhenAll(tasks);
        if (success)
            _botsJoined[m.Id_.Item] = true;
    }


    private async Task JoinBotToMatchmaking(Guid matchmakingId, bool isPremium, CancellationToken ct)
    {
        var nick = GenerateBotName(matchmakingId);


        if (isPremium)
        {
            var password = await premiumMatchmakingGames.GetPassword(matchmakingId);

            if (password is null)
            {
                throw new Exception("Password is null. Some conflict. It should not be reached.");
            }

            var cmd = new App.Application.UseCase.Matchmaking.JoinPremiumMatchmaking.Command(nick, true, password);

            try
            {
                var (id, corrected, pid) =
                    await bus.SendAsync<
                        App.Application.UseCase.Matchmaking.JoinPremiumMatchmaking.Command,
                        App.Application.UseCase.Matchmaking.JoinPremiumMatchmaking.Result>(cmd, ct);

                _usedNicks.GetOrAdd(id, _ => []).Add(corrected);
                log.Debug($"Bot {corrected} joined {id} (PREMIUM) (playerId={pid})");
            }
            catch (Exception ex) when (ex is
                                           App.Application.UseCase.Matchmaking.JoinPremiumMatchmaking
                                               .RoomIsFullException or
                                           App.Application.UseCase.Matchmaking.JoinPremiumMatchmaking
                                               .PrivateServerInUse)
            {
            }
            catch (Exception ex)
            {
                log.Error("Bot failed to join", ex);
            }
        }
        else
        {
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
                                           App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.RoomIsFullException
                                           or
                                           App.Application.UseCase.Matchmaking.JoinQuickMatchmaking
                                               .MultipleGamesNotSupportedException)
            {
            }
            catch (Exception ex)
            {
                log.Error("Bot failed to join", ex);
            }
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