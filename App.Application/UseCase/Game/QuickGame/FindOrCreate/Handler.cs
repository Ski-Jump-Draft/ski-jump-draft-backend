using App.Application.Abstractions;
using App.Application.Exception;
using App.Application.Ext;
using App.Application.Projection;
using App.Application.UseCase.Game.Exception;
using App.Domain.Game;
using App.Domain.Shared;
using App.Domain.Repositories;
using App.Domain.Repository;
using GameErrors = App.Domain.Game.GameModule.Error; // taki fajny myk

namespace App.Application.UseCase.Game.QuickGame.FindOrCreate;

// TODO: Walidacja i nie raw string!
public record Command(
    string Nick
) : ICommand<Guid>;

public class Handler(
    IGameRepository games,
    IGamesProjection gamesProjection,
    Func<Guid> getGlobalHostId,
    Func<Settings.Settings> getSettings,
    IGuid guid
) : IApplicationHandler<Guid, Command>
{
    public async Task<Guid> HandleAsync(Command command, CancellationToken ct)
    {
        var activeGames = (await gamesProjection.GetActiveGamesAsync()).ToArray();
        switch (activeGames.Length)
        {
            case 1:
            {
                var activeGame = activeGames.Single();
                var matchmakingInfo = await gamesProjection.GetGameMatchmakingInfo(activeGame.GameId);
                if (matchmakingInfo == null)
                    throw new JoiningQuickGameFailedException(command.Nick, Reason.GameAlreadyRunning);
                var gameId = App.Domain.Game.Id.Id.NewId(matchmakingInfo.GameId);
                return gameId.Item;
            }
            case > 1:
                // TODO: Dynamicznie wybieramy serwer - wg dostępności, regionu
                throw new NotImplementedException();
            default:
            {
                var gameId = Id.Id.NewId(guid.NewGuid());
                var aggregateVersion = AggregateVersion.AggregateVersion.NewAggregateVersion(0u);
                var hostId = App.Domain.Game.Hosting.HostModule.Id.NewId(getGlobalHostId());
                var settings = getSettings();
                var game = Domain.Game.Game.Create(gameId, aggregateVersion, hostId, settings);
                if (!game.IsOk)
                    throw new JoiningQuickGameFailedException(command.Nick, Reason.ErrorDuringSettingUpGame);
                
                var (gameAggregate, events) = game.ResultValue;

                var correlationId = guid.NewGuid();
                var causationId = correlationId;
                var expectedVersion = gameAggregate.Version_;
                await FSharpAsyncExt.AwaitOrThrow(
                    games.SaveAsync(gameAggregate, events, expectedVersion, correlationId, causationId, ct),
                    new JoiningQuickGameFailedException(command.Nick, Reason.ErrorDuringSettingUpGame), ct);
                return gameId.Item;
            }
        }
    }
}