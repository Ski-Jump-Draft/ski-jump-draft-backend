using System.Collections.Concurrent;
using App.Application.Commanding;
using App.Application.Extensions;
using App.Application.Utility;
using App.Domain.Matchmaking;

namespace App.Web.HostedServices.RealFlow;

public class OnlineBotJoiner(IMatchmakings matchmakings, ICommandBus bus, IRandom random, IMyLogger log)
    : BackgroundService
{
    private readonly ConcurrentDictionary<Guid, ConcurrentBag<string>> _usedBotNicksByMatchmaking = new();

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var delayMilliseconds = random.Gaussian(4000, 500);
            await Task.Delay(TimeSpan.FromMilliseconds(delayMilliseconds), ct);

            var all = await matchmakings.GetInProgress(null, ct);
            var matchmaking = all.FirstOrDefault();

            if (matchmaking is null)
            {
                continue;
            }

            var minPlayersCount = SettingsModule.MinPlayersModule.value(matchmaking.MinPlayersCount);
            var currentPlayersCount = matchmaking.PlayersCount;
            var botCanJoin = currentPlayersCount < minPlayersCount;

            if (!botCanJoin)
            {
                continue;
            }

            var nick = GenerateBotName(matchmaking.Id_.Item);

            var command = new App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.Command(nick, IsBot: true);

            try
            {
                var (matchmakingId, correctedNick, playerId) = await bus
                    .SendAsync<App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.Command,
                        App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.Result>(command, ct);

                _usedBotNicksByMatchmaking[matchmakingId]?.Add(correctedNick);

                log.Debug($"Bot {correctedNick} joined {matchmakingId}");
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