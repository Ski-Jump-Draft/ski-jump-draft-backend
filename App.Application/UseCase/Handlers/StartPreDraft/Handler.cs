using App.Application.Commanding;
using App.Application.Exception;
using App.Application.Ext;
using App.Application.Factory;
using App.Application.UseCase.Exception;
using App.Application.UseCase.Helper;
using App.Domain.Shared;
using App.Domain.Repositories;
using Microsoft.FSharp.Collections;

namespace App.Application.UseCase.Handlers.StartPreDraft;

public record Result(Domain.PreDraft.Id.Id PreDraftId, Domain.SimpleCompetition.CompetitionId FirstCompetitionId);

public record Command(
    Domain.Game.Id.Id GameId
) : ICommand<Result>;

public class Handler(
    IGameRepository games,
    IPreDraftRepository preDrafts,
    ICompetitionRepository competitions,
    IQuickGamePreDraftSettingsProvider preDraftSettingsProvider,
    IPreDraftCompetitionFactory preDraftCompetitionFactory,
    IGuid guid
) : ICommandHandler<Command, Result>
{
    public async Task<Result> HandleAsync(Command command, MessageContext messageContext,
        CancellationToken ct)
    {
        var game = await games.LoadAsync(command.GameId, ct)
            .AwaitOrWrap(_ => new IdNotFoundException<Guid>(command.GameId.Item));

        var newPreDraftId = Domain.PreDraft.Id.Id.NewId(guid.NewGuid());
        var firstCompetitionId = Domain.SimpleCompetition.CompetitionId.NewCompetitionId(guid.NewGuid());

        var (_, firstCompetitionCreationEvents) = preDraftCompetitionFactory.Create(newPreDraftId);
        var preDraftSettings = await preDraftSettingsProvider.Provide();

        var preDraftCreationResult = Domain.PreDraft.PreDraft.Create(newPreDraftId, AggregateVersion.zero,
            preDraftSettings, firstCompetitionId);

        if (!preDraftCreationResult.IsOk)
            throw new PreDraftStartingFailedException("Error during PreDraft creation", command.GameId);

        var (preDraft, preDraftEvents) = preDraftCreationResult.ResultValue;
        var expectedVersion = preDraft.Version_;
        await preDrafts.SaveAsync(preDraft.Id_, preDraftEvents, expectedVersion, messageContext.CorrelationId,
            messageContext.CausationId, ct);

        await competitions.SaveAsync(firstCompetitionId, ListModule.OfSeq(firstCompetitionCreationEvents),
            AggregateVersion.zero,
            messageContext.CorrelationId, messageContext.CausationId, ct);

        var gameStartPreDraftResult = game.StartPreDraft(preDraft.Id_);
        if (!gameStartPreDraftResult.IsOk)
            throw new PreDraftStartingFailedException("Error during starting the PreDraft in Game aggregate",
                command.GameId);

        var (gameAfterStartingPreDraft, gameEvents) = gameStartPreDraftResult.ResultValue;
        expectedVersion = game.Version_;
        await games.SaveAsync(gameAfterStartingPreDraft.Id_, gameEvents, expectedVersion,
            messageContext.CorrelationId,
            messageContext.CausationId, ct);

        return new Result(preDraft.Id_, firstCompetitionId);
    }
}