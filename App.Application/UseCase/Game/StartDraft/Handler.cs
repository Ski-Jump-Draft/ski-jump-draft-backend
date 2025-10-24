using App.Application.Commanding;
using App.Application.Exceptions;
using App.Application.Extensions;
using App.Application.Game.DraftTurnIndexes;
using App.Application.Messaging.Notifiers;
using App.Application.Messaging.Notifiers.Mapper;
using App.Application.Service;
using App.Application.Utility;
using App.Domain.Game;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;

namespace App.Application.UseCase.Game.StartDraft;

public record Command(
    Guid GameId
) : ICommand<Result>;

public record Result();

public class Handler(
    IGames games,
    IGameNotifier gameNotifier,
    IMyLogger logger,
    IRandom random,
    GameUpdatedDtoMapper gameUpdatedDtoMapper,
    DraftSystemSchedulerService draftSystemSchedulerService,
    IDraftTurnIndexesArchive draftTurnIndexesArchive)
    : ICommandHandler<Command, Result>
{
    public async Task<Result> HandleAsync(Command command, CancellationToken ct)
    {
        var game = await games.GetById(GameId.NewGameId(command.GameId), ct)
            .AwaitOrWrap(_ => new IdNotFoundException(command.GameId));

        logger.Info($"Starting draft for game {game.Id_.Item}");


        var shuffleFunOption =
            FSharpOption<FSharpFunc<Tuple<FSharpList<PlayerId>, int>, FSharpList<PlayerId>>>.Some(
                CreateShufflePlayersFunction(random));

        var gameAfterStartDraftResult = game.StartDraft(shuffleFunOption);

        if (!gameAfterStartDraftResult.IsOk) return new Result();

        var gameAfterStartDraft = gameAfterStartDraftResult.ResultValue;

        await games.Add(gameAfterStartDraft, ct);

        await ArchiveDraftTurnIndexesIfNeeded(command, game);

        await draftSystemSchedulerService.ScheduleSystemDraftEvents(gameAfterStartDraft, ct);

        await gameNotifier.GameUpdated(await gameUpdatedDtoMapper.FromDomain(gameAfterStartDraft, ct: ct));

        return new Result();
    }

    private async Task ArchiveDraftTurnIndexesIfNeeded(Command command, Domain.Game.Game game)
    {
        if (!game.Settings.DraftSettings.Order.IsRandom)
        {
            var players = PlayersModule.toIdsList(game.Players);
            var fixedTurnIndexesDtos =
                players.Select((playerId, index) => new DraftFixedTurnIndexDto(playerId.Item, index)).ToList();
            await draftTurnIndexesArchive.SetFixedAsync(command.GameId, fixedTurnIndexesDtos);
        }
    }

    private static FSharpFunc<Tuple<FSharpList<PlayerId>, int>, FSharpList<PlayerId>> CreateShufflePlayersFunction(
        IRandom random)
    {
        Converter<Tuple<FSharpList<PlayerId>, int>, FSharpList<PlayerId>> conv = tuple =>
        {
            var playersList = tuple.Item1;
            var count = tuple.Item2;

            var arr = playersList.ToArray();

            lock (random)
            {
                for (var i = arr.Length - 1; i > 0; i--)
                {
                    var j = random.Next(i + 1);
                    (arr[j], arr[i]) = (arr[i], arr[j]);
                }
            }

            var take = (count <= 0 || count > arr.Length) ? arr : arr.Take(count).ToArray();

            return ListModule.OfSeq(take);
        };

        var fsharpFunc = FSharpFunc<Tuple<FSharpList<PlayerId>, int>, FSharpList<PlayerId>>.FromConverter(conv);
        return fsharpFunc;
    }
}