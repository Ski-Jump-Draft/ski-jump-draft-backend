using System.Collections.Immutable;
using App.Application.Acl;
using App.Application.Commanding;
using App.Application.Exceptions;
using App.Application.Extensions;
using App.Application.Game.Gate;
using App.Application.Mapping;
using App.Application.Messaging.Notifiers;
using App.Application.Messaging.Notifiers.Mapper;
using App.Application.Policy.GameHillSelector;
using App.Application.Policy.GameJumpersSelector;
using App.Application.Utility;
using App.Domain.Competition;
using App.Domain.Game;
using App.Domain.GameWorld;
using Microsoft.FSharp.Collections;
using HillId = App.Domain.GameWorld.HillId;
using JumperId = App.Domain.GameWorld.JumperId;

namespace App.Application.UseCase.Game.StartMainCompetition;

public record Command(
    Guid GameId
) : ICommand<Result>;

public record Result(Guid CompetitionId);

public class Handler(
    IJson json,
    IGames games,
    IGameNotifier gameNotifier,
    IScheduler scheduler,
    IClock clock,
    IGuid guid,
    ICompetitionJumperAcl competitionJumperAcl,
    ISelectGameStartingGateService selectGameStartingGateService,
    IMyLogger logger,
    GameUpdatedDtoMapper gameUpdatedDtoMapper)
    : ICommandHandler<Command, Result>
{
    public async Task<Result> HandleAsync(Command command, CancellationToken ct)
    {
        var game = await games.GetById(GameId.NewGameId(command.GameId), ct)
            .AwaitOrWrap(_ => new IdNotFoundException(command.GameId));

        if (!game.WaitsForNextMainCompetition)
        {
            throw new Exception("Game is not waiting for the main competition");
        }

        logger.Info($"Starting main competition for game {game.Id_.Item}");

        var competitionGuid = guid.NewGuid();
        var competitionId = CompetitionId.NewCompetitionId(competitionGuid);
        var competitionJumpers = game.Jumpers.ToCompetitionJumpers(competitionJumperAcl).ToImmutableList();
        var startingGateInt = await selectGameStartingGateService.Select(competitionJumpers, game.Hill.Value, ct);
        var startingGate = Domain.Competition.Gate.NewGate(startingGateInt);

        var gameAfterMainCompetitionStartResult =
            game.StartMainCompetition(competitionId, ListModule.OfSeq(competitionJumpers), startingGate);
        if (!gameAfterMainCompetitionStartResult.IsOk)
            throw new Exception("Game start next pre draft competition failed",
                new Exception(gameAfterMainCompetitionStartResult.ErrorValue.ToString()));

        var gameAfterMainCompetitionStart = gameAfterMainCompetitionStartResult.ResultValue;
        await games.Add(gameAfterMainCompetitionStart, ct);
        var now = clock.Now();
        await scheduler.ScheduleAsync(
            jobType: "SimulateJumpInGame",
            payloadJson: json.Serialize(new { GameId = game.Id_.Item }),
            runAt: now.AddSeconds(4),
            uniqueKey: $"SimulateJumpInGame:{game.Id_.Item}_{now.ToUnixTimeSeconds()}",
            ct: ct);
        await gameNotifier.GameUpdated(await gameUpdatedDtoMapper.FromDomain(gameAfterMainCompetitionStart));
        return new Result(competitionGuid);
    }
}