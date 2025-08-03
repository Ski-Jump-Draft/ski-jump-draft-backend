using App.Application.Commanding;
using App.Application.Exception;
using App.Application.Ext;
using App.Application.ReadModel.Projection;
using App.Application.UseCase.Game.Exception;
using App.Application.UseCase.Helper;
using App.Domain.Game;
using App.Domain.Repositories;
using App.Domain.Shared;
using Microsoft.FSharp.Collections;
using Random = App.Domain.Shared.Random;

namespace App.Application.UseCase.Game.StartDraftPhase;

public record Command(Id.Id GameId, Domain.Draft.Settings.Settings DraftSettings) : ICommand;

public class Handler(
    IGuid guid,
    Random.IRandom random,
    IQuickGameJumpersSelector jumpersSelector,
    IGameRepository games,
    IDraftRepository drafts,
    IGameParticipantsProjection gameParticipantsProjection,
    IDraftParticipantsFactory draftParticipantsFactory,
    IDraftSubjectsFactory draftSubjectsFactory)
    : ICommandHandler<Command>
{
    public async Task HandleAsync(Command command, MessageContext messageContext, CancellationToken ct)
    {
        var game = await games.LoadAsync(command.GameId, ct)
            .AwaitOrWrap(_ => new IdNotFoundException<Guid>(command.GameId.Item));

        var gameParticipants = await gameParticipantsProjection.GetParticipantsByGameIdAsync(game.Id_);

        var draftParticipants = draftParticipantsFactory.CreateFromDtos(gameParticipants);

        var gameWorldJumpers = jumpersSelector.Select();
        var draftSubjects = draftSubjectsFactory.CreateIndividuals(gameWorldJumpers);

        var draftId = Domain.Draft.Id.Id.NewId(guid.NewGuid());
        var initialAggregateVersion = AggregateVersion.zero;
        var draftSeed = random.NextUInt64();
        var newDraftResult = Domain.Draft.Draft.Create(draftId, initialAggregateVersion, command.DraftSettings,
            ListModule.OfSeq(draftParticipants), ListModule.OfSeq(draftSubjects), draftSeed);

        if (!newDraftResult.IsOk)
        {
            throw new DraftCreationFailedException(command.GameId, command.DraftSettings);
        }

        var (draftAggregate, draftEvents) = newDraftResult.ResultValue;
        var draftExpectedVersion = draftAggregate.Version_;
        await drafts.SaveAsync(draftAggregate.Id_, draftEvents, draftExpectedVersion, messageContext.CorrelationId,
            messageContext.CausationId, ct);

        var startDraftResult = game.StartDraft(draftId);
        if (!startDraftResult.IsOk)
        {
            throw new DraftCreationFailedException(command.GameId, command.DraftSettings);
        }

        var (gameAggregate, gameEvents) = startDraftResult.ResultValue;

        var expectedVersion = game.Version_;

        await games.SaveAsync(gameAggregate.Id_, gameEvents, expectedVersion, messageContext.CorrelationId,
            messageContext.CausationId, ct);
    }
}