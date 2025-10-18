using System.Collections.Immutable;
using App.Application.Acl;
using App.Application.Commanding;
using App.Application.Exceptions;
using App.Application.Extensions;
using App.Application.Game;
using App.Application.Game.GameGateSelectionPack;
using App.Application.Game.Gate;
using App.Application.Mapping;
using App.Application.Messaging.Notifiers;
using App.Application.Messaging.Notifiers.Mapper;
using App.Application.Policy.GameCompetitionStartlist;
using App.Application.Policy.GameHillSelector;
using App.Application.Policy.GameJumpersSelector;
using App.Application.Utility;
using App.Domain.Competition;
using App.Domain.Game;
using App.Domain.GameWorld;
using Microsoft.FSharp.Collections;
using HillId = App.Domain.GameWorld.HillId;
using Jumper = App.Domain.Competition.Jumper;
using JumperId = App.Domain.GameWorld.JumperId;
using PreDraftDto = App.Application.Messaging.Notifiers.PreDraftDto;

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
    IMyLogger logger,
    GameUpdatedDtoMapper gameUpdatedDtoMapper,
    IGameSchedule gameSchedule,
    IGameCompetitionStartlist gameCompetitionStartlist,
    IGameGateSelectionPack gameGateSelectionPack)
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

        var competitionJumpersStartlist = await GenerateCompetitionJumpersStartlist(command.GameId, ct);

        var startingGate = await SelectStartingGate(competitionJumpersStartlist, game, ct);

        var gameAfterMainCompetitionStartResult =
            game.StartMainCompetition(competitionId, ListModule.OfSeq(competitionJumpersStartlist), startingGate);
        if (!gameAfterMainCompetitionStartResult.IsOk)
            throw new Exception("Game start next pre draft competition failed",
                new Exception(gameAfterMainCompetitionStartResult.ErrorValue.ToString()));

        var gameAfterMainCompetitionStart = gameAfterMainCompetitionStartResult.ResultValue;
        await games.Add(gameAfterMainCompetitionStart, ct);
        var timeToJump = game.Settings.CompetitionJumpInterval.Value;
        var now = clock.Now();
        gameSchedule.ScheduleEvent(command.GameId, GameScheduleTarget.CompetitionJump, timeToJump);
        await scheduler.ScheduleAsync(
            jobType: "SimulateJumpInGame",
            payloadJson: json.Serialize(new { GameId = game.Id_.Item }),
            runAt: now.Add(timeToJump),
            uniqueKey: $"SimulateJumpInGame:{game.Id_.Item}_{now.ToUnixTimeSeconds()}",
            ct: ct);
        await gameNotifier.GameUpdated(await gameUpdatedDtoMapper.FromDomain(gameAfterMainCompetitionStart, ct: ct));
        return new Result(competitionGuid);
    }

    private async Task<Gate> SelectStartingGate(ImmutableList<Jumper> competitionJumpersStartlist,
        Domain.Game.Game game, CancellationToken ct)
    {
        var gateSelectionPack =
            await gameGateSelectionPack.GetForCompetition(game.Id.Item, competitionJumpersStartlist, game.Hill.Value,
                ct);
        var startingGateSelector = gateSelectionPack.StartingGateSelector;
        var startingGateInt = startingGateSelector.Select();
        var startingGate = Domain.Competition.Gate.NewGate(startingGateInt);
        return startingGate;
    }


    private async Task<ImmutableList<Jumper>> GenerateCompetitionJumpersStartlist(Guid gameId, CancellationToken ct)
    {
        var gameJumpersStartlist = await gameCompetitionStartlist.Get(gameId, new MainCompetitionDto(), ct);
        var competitionJumpersStartlist =
            gameJumpersStartlist.ToCompetitionJumpers(competitionJumperAcl, gameId).ToImmutableList();
        return competitionJumpersStartlist;
    }
}