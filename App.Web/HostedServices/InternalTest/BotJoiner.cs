using System.Collections.Concurrent;
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
    private readonly ConcurrentDictionary<Guid, ConcurrentBag<string>> _usedBotNicksByMatchmaking = new();
    private readonly ConcurrentDictionary<Guid, bool> _botsHaveJoined = new();

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var all = await matchmakings.GetInProgress(ct);
            var matchmaking = all.FirstOrDefault();

            if (matchmaking is null)
            {
                continue;
            }

            var matchmakingId = matchmaking.Id_.Item;

            var now = clock.Now();
            var remainingTime = matchmaking.RemainingTime(now);
            if (remainingTime is null) continue;

            var remainingSlots = matchmaking.RemainingSlots;
            var botsHaveJoinedInThisMatchmaking = _botsHaveJoined.ContainsKey(matchmaking.Id_.Item);
            var botsShouldJoin = remainingTime.Value.TotalSeconds < 5 && remainingSlots > 0 &&
                                 !botsHaveJoinedInThisMatchmaking;

            if (!botsShouldJoin) continue;
            _botsHaveJoined[matchmaking.Id_.Item] = true;
            var botJoinInterval = TimeSpan.FromMilliseconds(100);
            var botsToJoin = (int)Math.Floor((double)remainingSlots / 2);
            for (var i = 0; i < botsToJoin; i++)
            {
                await Task.Delay(botJoinInterval, ct);
                await JoinBotToMatchmaking(matchmakingId, ct);
            }
        }
    }

    private async Task JoinBotToMatchmaking(Guid matchmakingGuid, CancellationToken ct)
    {
        var nick = GenerateBotName(matchmakingGuid);

        var command = new App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.Command(nick, IsBot: true);

        try
        {
            var (matchmakingId, correctedNick, playerId) = await bus
                .SendAsync<App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.Command,
                    App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.Result>(command, ct);

            _usedBotNicksByMatchmaking[matchmakingId]?.Add(correctedNick);

            log.Debug($"Bot {correctedNick} joined {matchmakingId} (playerId = {playerId})");
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

    private string GenerateBotName(Guid? matchmakingId)
    {
        List<string> maleNames =
        [
            "Marek", "Jakub", "Jan", "Piotr", "Paweł",
            "Krzysztof", "Tomasz", "Adam", "Andrzej", "Michał",
            "Łukasz", "Mateusz", "Maciej", "Marcin", "Grzegorz",
            "Rafał", "Kamil", "Dawid", "Patryk", "Artur"
        ];

        List<string> femaleNames =
        [
            "Anna", "Maria", "Katarzyna", "Agnieszka", "Małgorzata",
            "Ewa", "Magdalena", "Joanna", "Monika", "Aleksandra",
            "Barbara", "Beata", "Natalia", "Karolina", "Dorota",
            "Sylwia", "Paulina", "Justyna", "Elżbieta", "Weronika"
        ];

        var allNames = maleNames.Concat(femaleNames).ToList();

        if (matchmakingId is null)
            return $"Bot {allNames.GetRandomElement(random)}";

        var usedNames = _usedBotNicksByMatchmaking.GetOrAdd(matchmakingId.Value, _ => []);
        var allowedNames = allNames
            .Select(name => $"Bot {name}")
            .Except(usedNames)
            .ToList();


        if (allowedNames.Count == 0)
            return "Bot";

        var chosen = allowedNames.GetRandomElement(random);
        usedNames.Add(chosen);
        return chosen;
    }
}