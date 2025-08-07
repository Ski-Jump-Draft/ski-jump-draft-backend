using App.Application.Commanding;
using App.Application.Exception;
using App.Application.Ext;
using App.Application.ReadModel.Projection;
using App.Application.UseCase.Exception;
using App.Application.UseCase.Helper;
using App.Domain.Game;
using App.Domain.Repositories;
using App.Domain.Shared;
using Microsoft.FSharp.Collections;
using Random = App.Domain.Shared.Random;

namespace App.Application.UseCase.Handlers.StartDraft;

public record Command(Id.Id GameId) : ICommand;

public class Handler(
    IGuid guid,
    Random.IRandom random,
    IQuickGameJumpersSelector jumpersSelector,
    IGameWorldJumperQuery gameWorldJumperQuery,
    IGameRepository games,
    IDraftRepository drafts,
    IGameParticipantsProjection gameParticipantsProjection,
    IDraftParticipantsFactory draftParticipantsFactory,
    IDraftSubjectsFactory draftSubjectsFactory,
    IQuickGameDraftSettingsProvider draftSettingsProvider)
    : ICommandHandler<Command>
{
    public async Task HandleAsync(Command command, MessageContext messageContext, CancellationToken ct)
    {
        var game = await games.LoadAsync(command.GameId, ct)
            .AwaitOrWrap(_ => new IdNotFoundException<Guid>(command.GameId.Item));

        var gameParticipants = await gameParticipantsProjection.GetParticipantsByGameIdAsync(game.Id_);

        var draftParticipants = draftParticipantsFactory.CreateFromDtos(gameParticipants);

        var gameWorldJumperIds = await jumpersSelector.Select();
        var gameWorldJumpers = await gameWorldJumperQuery.GetByIds(gameWorldJumperIds);
        var draftSubjects = draftSubjectsFactory.CreateIndividuals(gameWorldJumpers);

        var draftSettings = await draftSettingsProvider.Provide();
        var draftId = Domain.Draft.Id.Id.NewId(guid.NewGuid());
        var initialAggregateVersion = AggregateVersion.zero;
        var draftSeed = random.NextUInt64();
        var newDraftResult = Domain.Draft.Draft.Create(draftId, initialAggregateVersion, draftSettings,
            ListModule.OfSeq(draftParticipants), ListModule.OfSeq(draftSubjects), draftSeed);

        if (!newDraftResult.IsOk)
        {
            throw new DraftCreationFailedException("Error during creating a new draft", command.GameId, draftSettings);
        }

        var (draftAggregate, draftEvents) = newDraftResult.ResultValue;
        var draftExpectedVersion = draftAggregate.Version_;
        await drafts.SaveAsync(draftAggregate.Id_, draftEvents, draftExpectedVersion, messageContext.CorrelationId,
            messageContext.CausationId, ct);

        var startDraftResult = game.StartDraft(draftId);
        if (!startDraftResult.IsOk)
        {
            throw new DraftCreationFailedException("Error during starting the new draft in the Game aggregate",
                command.GameId,
                draftSettings);
        }

        var (gameAggregate, gameEvents) = startDraftResult.ResultValue;

        var expectedVersion = game.Version_;

        await games.SaveAsync(gameAggregate.Id_, gameEvents, expectedVersion, messageContext.CorrelationId,
            messageContext.CausationId, ct);
    }
}