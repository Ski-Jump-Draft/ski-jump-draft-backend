using App.Application.Commanding;
using App.Application.Exception;
using App.Application.Ext;
using App.Application.Factory;
using App.Application.UseCase.Exception;
using App.Domain.Shared;
using App.Domain.Repositories;
using Microsoft.FSharp.Collections;

namespace App.Application.UseCase.Handlers.ContinuePreDraft;

public record Command(
    Domain.Game.Id.Id GameId
) : ICommand<Domain.SimpleCompetition.CompetitionId>;

public class Handler(
    IGameRepository games,
    IPreDraftRepository preDrafts,
    ICompetitionRepository competitions,
    IPreDraftCompetitionFactory preDraftCompetitionFactory,
    IGuid guid
) : ICommandHandler<Command, Domain.SimpleCompetition.CompetitionId>
{
    public async Task<Domain.SimpleCompetition.CompetitionId> HandleAsync(Command command,
        MessageContext messageContext,
        CancellationToken ct)
    {
        var game = await games.LoadAsync(command.GameId, ct)
            .AwaitOrWrap(_ => new IdNotFoundException<Guid>(command.GameId.Item));

        if (!game.Phase_.IsPreDraft)
        {
            throw new InvalidOperationException(">" + game.Phase_ + "< is not a pre-draft phase (game id: " +
                                                command.GameId + ")");
        }

        var preDraftPhase = (Domain.Game.GameModule.Phase.PreDraft)game.Phase_;
        var preDraftId = preDraftPhase.Item;

        var preDraft = await preDrafts.LoadAsync(preDraftId, ct)
            .AwaitOrWrap(_ => new IdNotFoundException<Guid>(preDraftId.Item));

        var nextCompetitionId = Domain.SimpleCompetition.CompetitionId.NewCompetitionId(guid.NewGuid());

        var (_, competitionCreationEvents) = preDraftCompetitionFactory.Create(preDraftId);

        var preDraftContinueResult = preDraft.Advance(nextCompetitionId);

        if (!preDraftContinueResult.IsOk)
            throw new PreDraftStartingFailedException("Error during PreDraft creation", command.GameId);

        var (preDraftAfterContinue, preDraftEvents) = preDraftContinueResult.ResultValue;
        var expectedVersion = preDraft.Version_;
        await preDrafts.SaveAsync(preDraft.Id_, preDraftEvents, expectedVersion, messageContext.CorrelationId,
            messageContext.CausationId, ct);

        await competitions.SaveAsync(nextCompetitionId, ListModule.OfSeq(competitionCreationEvents),
            AggregateVersion.zero,
            messageContext.CorrelationId, messageContext.CausationId, ct);


        return nextCompetitionId;
    }
}