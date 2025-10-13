using System.Collections.Concurrent;
using System.Collections.Immutable;
using App.Application.Commanding;
using App.Application.Extensions;
using App.Application.Matchmaking;
using App.Application.Utility;
using App.Domain.Matchmaking;

namespace App.Web.HostedServices.MockedFlow;

public class MockedOnlineBotJoiner(
    IMatchmakings repo,
    ICommandBus bus,
    IRandom random,
    IMyLogger log,
    IClock clock,
    IPremiumMatchmakings premiumMatchmakings)
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
            var all = (await repo.GetInProgress(ct)).ToImmutableArray();
            var now = clock.Now();

            var tasks = all
                .Where(matchmaking => IsEligible(matchmaking, now))
                .Select(matchmaking => JoinBots(matchmaking, ct));

            await Task.WhenAll(tasks);
            await Task.Delay(3000, ct);
        }
    }

    private bool IsEligible(Matchmaking m, DateTimeOffset now)
        => !_botsJoined.ContainsKey(m.Id_.Item)
           && m.RemainingSlots > 0;

    private async Task JoinBots(Matchmaking m, CancellationToken ct)
    {
        _botsJoined[m.Id_.Item] = true;
        var botsToJoin = Math.Max(1, m.RemainingSlots / 2);

        var isPremium = await premiumMatchmakings.PremiumMatchmakingIsRunning(m.Id_.Item);

        var tasks = Enumerable.Range(0, botsToJoin)
            .Select(async i =>
            {
                await Task.Delay(250 * i, ct);
                if (isPremium)
                {
                    await JoinSingleBotPremium(m.Id_.Item, ct);
                }
                else
                {
                    await JoinSingleBot(m.Id_.Item, ct);
                }
            });

        await Task.WhenAll(tasks);
    }

    private async Task JoinSingleBot(Guid matchId, CancellationToken ct)
    {
        var nick = GenerateBotName(matchId);
        var cmd = new App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.Command(nick, IsBot: true);

        try
        {
            var (id, corrected, pid) =
                await bus.SendAsync<
                    App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.Command,
                    App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.Result>(cmd, ct);

            _usedNicks.GetOrAdd(id, _ => []).Add(corrected);
            log.Debug($"Bot {corrected} joined {id}, playerId={pid})");
        }
        catch (App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.RoomIsFullException)
        {
        }
        catch (App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.MultipleGamesNotSupportedException)
        {
        }
        catch (Exception ex)
        {
            log.Error("Bot failed to join", ex);
        }
    }

    private async Task JoinSingleBotPremium(Guid matchmakingId, CancellationToken ct)
    {
        var nick = GenerateBotName(matchmakingId);
        var password = await premiumMatchmakings.GetPassword(matchmakingId);
        if (password is null)
        {
            throw new Exception("Password is null. Some conflict. It should not be reached.");
        }

        var cmd = new App.Application.UseCase.Matchmaking.JoinPremiumMatchmaking.Command(nick, Password: password,
            IsBot: true);

        try
        {
            var (id, corrected, pid) =
                await bus.SendAsync<
                    App.Application.UseCase.Matchmaking.JoinPremiumMatchmaking.Command,
                    App.Application.UseCase.Matchmaking.JoinPremiumMatchmaking.Result>(cmd, ct);

            _usedNicks.GetOrAdd(id, _ => []).Add(corrected);
            log.Debug($"(Premium) Bot {corrected} joined {id}, playerId={pid})");
        }
        catch (App.Application.UseCase.Matchmaking.JoinPremiumMatchmaking.RoomIsFullException)
        {
        }
        catch (Exception ex)
        {
            log.Error("(Premium) Bot failed to join", ex);
        }
    }

    private string GenerateBotName(Guid id)
    {
        var used = _usedNicks.GetOrAdd(id, _ => new());
        var pool = AllNames.Except(used).ToList();
        var baseName = pool.Count == 0 ? "Bot" : pool.GetRandomElement(random);
        var final = $"{baseName}";
        used.Add(final);
        return final;
    }
}