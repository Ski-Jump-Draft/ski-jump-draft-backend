using App.Application.Acl;
using App.Application.Bot;
using App.Application.Commanding;
using App.Application.Exceptions;
using App.Application.Extensions;
using App.Application.Game;
using App.Application.Game.DraftPicks;
using App.Application.Messaging.Notifiers;
using App.Application.Messaging.Notifiers.Mapper;
using App.Application.Policy.DraftPicker;
using App.Application.Utility;
using App.Domain.Game;
using App.Domain.GameWorld;
using Microsoft.FSharp.Collections;
using JumperId = App.Domain.GameWorld.JumperId;

namespace App.Application.UseCase.Game.PassPick;

public record Command(
    Guid GameId,
    Guid PlayerId,
    int TurnIndex
) : ICommand<Result>;

public record Result(Guid JumperId);

public class Handler(
    IGames games,
    IMyLogger logger,
    IDraftPassPicker passPicker,
    ICommandBus commandBus,
    IBotPickLock botPickLock)
    : ICommandHandler<Command, Result>
{
    public async Task<Result> HandleAsync(Command command, CancellationToken ct)
    {
        var passPickIsLocked = botPickLock.IsLocked(command.GameId, command.PlayerId);
        if (passPickIsLocked)
        {
            logger.Warn($"Tried to pass-pick, but pass-pick is locked (Game: ${command.GameId}, Player: ${
                command.PlayerId}).");
            throw new PassPickingLockedException(command.GameId, command.PlayerId);
        }

        var game = await games.GetById(GameId.NewGameId(command.GameId), ct)
            .AwaitOrWrap(_ => new IdNotFoundException(command.GameId));


        var isAppropriateTurn = game.CurrentTurnInDraft.IsSome() &&
                                (DraftModule.TurnIndexModule.value(game.CurrentTurnInDraft.Value.Index) ==
                                 command.TurnIndex);
        if (!isAppropriateTurn)
            throw new InvalidTurnIndexException(command.TurnIndex);

        var pickedGameJumperId = await passPicker.Pick(game, ct);

        logger.Info($"Pass-picking a Jumper ({pickedGameJumperId}) by a Player ({command.PlayerId}) in a Game ({
            command.GameId})");

        await commandBus.SendAsync<PickJumper.Command, PickJumper.Result>(new PickJumper.Command(command.GameId,
            command.PlayerId, pickedGameJumperId), ct);

        return new Result(pickedGameJumperId);
    }
}

public class PassPickingLockedException(Guid gameId, Guid playerId, string? message = null) : Exception(message);

public class InvalidTurnIndexException(int turnIndex, string? message = null) : Exception(message);