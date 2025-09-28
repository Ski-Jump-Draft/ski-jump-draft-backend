using System.Collections.Immutable;
using App.Application.Acl;
using App.Application.Commanding;
using App.Application.Exceptions;
using App.Application.Extensions;
using App.Application.Game;
using App.Application.Game.Gate;
using App.Application.Mapping;
using App.Application.Messaging.Notifiers;
using App.Application.Messaging.Notifiers.Mapper;
using App.Application.Policy;
using App.Application.Policy.GameCompetitionStartlist;
using App.Application.Utility;
using App.Domain.Competition;
using App.Domain.Game;
using Microsoft.FSharp.Collections;
using Jumper = App.Domain.Competition.Jumper;
using PreDraftDto = App.Application.Policy.GameCompetitionStartlist.PreDraftDto;

namespace App.Application.UseCase.Game.StartPreDraft;

public record Command(
    Guid GameId
) : ICommand<Result>;

public record Result(Guid CompetitionId);

public class Handler(
    IJson json,
    IGames games,
    ICompetitionJumperAcl competitionJumperAcl,
    IGameNotifier gameNotifier,
    IScheduler scheduler,
    IClock clock,
    IGuid guid,
    ISelectGameStartingGateService selectGameStartingGateService,
    GameUpdatedDtoMapper gameUpdatedDtoMapper,
    IGameSchedule gameSchedule,
    IGameCompetitionStartlist gameCompetitionStartlist,
    IMyLogger logger)
    : ICommandHandler<Command, Result>
{
    public async Task<Result> HandleAsync(Command command, CancellationToken ct)
    {
        var game = await games.GetById(Domain.Game.GameId.NewGameId(command.GameId), ct)
            .AwaitOrWrap(_ => new IdNotFoundException(command.GameId));
        logger.Info($"Starting pre draft for game {game.Id_.Item}");
        var competitionId = Domain.Competition.CompetitionId.NewCompetitionId(guid.NewGuid());
        var competitionJumpersStartlist = await GenerateCompetitionJumpersStartlist(command.GameId, ct);
        var startingGate = await SelectStartingGate(game, competitionJumpersStartlist, ct);
        var gameAfterPreDraftStartResult =
            game.StartPreDraft(competitionId, ListModule.OfSeq(competitionJumpersStartlist), startingGate);
        if (gameAfterPreDraftStartResult.IsOk)
        {
            var gameAfterPreDraftStart = gameAfterPreDraftStartResult.ResultValue;
            await games.Add(gameAfterPreDraftStart, ct);
            await ScheduleFirstCompetitionJump(game, ct);
            await gameNotifier.GameUpdated(await gameUpdatedDtoMapper.FromDomain(gameAfterPreDraftStart, ct: ct));
        }
        else
        {
            throw new Exception("Game start pre draft failed",
                new Exception(gameAfterPreDraftStartResult.ErrorValue.ToString()));
        }

        return new Result(command.GameId);
    }

    private async Task<Gate> SelectStartingGate(Domain.Game.Game game,
        ImmutableList<Jumper> competitionJumpersStartlist, CancellationToken ct)
    {
        var startingGateInt =
            await selectGameStartingGateService.Select(competitionJumpersStartlist, game.Hill.Value, ct);
        var startingGate = Domain.Competition.Gate.NewGate(startingGateInt);
        return startingGate;
    }

    private async Task<ImmutableList<Jumper>> GenerateCompetitionJumpersStartlist(Guid gameId, CancellationToken ct)
    {
        var gameJumpersStartlist = await gameCompetitionStartlist.Get(gameId, new PreDraftDto(0), ct);
        var competitionJumpersStartlist =
            gameJumpersStartlist.ToCompetitionJumpers(competitionJumperAcl).ToImmutableList();
        return competitionJumpersStartlist;
    }

    private async Task ScheduleFirstCompetitionJump(Domain.Game.Game game, CancellationToken ct)
    {
        var gameId = game.Id.Item;
        var timeToJump = game.Settings.CompetitionJumpInterval.Value;
        var now = clock.Now();
        gameSchedule.ScheduleEvent(gameId, GameScheduleTarget.CompetitionJump, timeToJump);
        await scheduler.ScheduleAsync(
            jobType: "SimulateJumpInGame",
            payloadJson: json.Serialize(new { GameId = gameId }),
            runAt: now.Add(timeToJump),
            uniqueKey: $"SimulateJumpInGame:{gameId}_{now.ToUnixTimeSeconds()}",
            ct: ct);
    }
}