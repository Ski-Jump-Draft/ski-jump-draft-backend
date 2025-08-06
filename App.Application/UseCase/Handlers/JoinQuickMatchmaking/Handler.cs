using System.Diagnostics;
using App.Application.Commanding;
using App.Application.Exception;
using App.Application.Ext;
using App.Application.ReadModel.Projection;
using App.Application.UseCase.Game.Exception;
using App.Application.UseCase.Helper;
using App.Domain.Matchmaking;
using App.Domain.Repositories;
using App.Domain.Shared;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;

namespace App.Application.UseCase.Handlers.JoinQuickMatchmaking;

public record Result(
    Domain.Matchmaking.Id MatchmakingId,
    Domain.Matchmaking.ParticipantModule.Id MatchmakingParticipantId);

public record Command(string Nick) : ICommand<Result>;

public class Handler(
    IMatchmakingRepository matchmakings,
    IActiveGamesProjection activeGamesProjection,
    IActiveMatchmakingsProjection activeMatchmakingsProjection,
    IQuickGameMatchmakingSettingsProvider matchmakingSettingsProvider,
    IMatchmakingParticipantFactory matchmakingParticipantFactory,
    IGuid guid
) : ICommandHandler<Command, Result>
{
    public async Task<Result> HandleAsync(Command command, MessageContext messageContext, CancellationToken ct)
    {
        await EnsureNoActiveGames(command, ct);

        var activeMatchmakings = await GetActiveMatchmakingsArray(ct);

        return activeMatchmakings.Length switch
        {
            1 => await JoinExistingMatchmaking(activeMatchmakings.Single(), command, messageContext, ct),
            > 1 => throw new NotImplementedException("Multiple active matchmakings aren't supported yet."),
            _ => await CreateAndJoinNewMatchmaking(command, messageContext, ct)
        };
    }

    private async Task<ActiveMatchmakingDto[]> GetActiveMatchmakingsArray(CancellationToken ct)
    {
        return (await activeMatchmakingsProjection.GetActiveMatchmakingsAsync(ct)).ToArray();
    }

    private async Task EnsureNoActiveGames(Command command, CancellationToken ct)
    {
        var activeGames = (await activeGamesProjection.GetActiveGamesAsync(ct)).ToArray();
        if (activeGames.Length > 0)
            throw new JoiningQuickMatchmakingFailedException(command.Nick,
                JoiningQuickMatchmakingFailReason.GameAlreadyRunning);
    }

    private async Task<Result> JoinExistingMatchmaking(
        App.Application.ReadModel.Projection.ActiveMatchmakingDto activeMatchmaking,
        Command command,
        MessageContext messageContext,
        CancellationToken ct)
    {
        var matchmakingId = Domain.Matchmaking.Id.NewId(activeMatchmaking.MatchmakingId);
        var matchmaking = await matchmakings.LoadAsync(matchmakingId, ct)
            .AwaitOrWrap(_ => new IdNotFoundException<Guid>(matchmakingId.Item));

        var matchmakingParticipant = matchmakingParticipantFactory.CreateFromNick(command.Nick);
        var matchmakingJoinResult = matchmaking.Join(matchmakingParticipant);

        var matchmakingAndEvents = CreateValueTupleForMatchmakingAndEvents(matchmakingJoinResult);

        return matchmakingJoinResult.IsOk
            ? await SaveJoinAndReturnResult(matchmakingAndEvents, messageContext,
                matchmakingParticipant.Id, ct)
            : throw TranslateDomainError(matchmakingJoinResult.ErrorValue, matchmaking, matchmakingParticipant);
    }

    private async Task<Result> CreateAndJoinNewMatchmaking(
        Command command,
        MessageContext messageContext,
        CancellationToken ct)
    {
        var newMatchmakingId = Domain.Matchmaking.Id.NewId(guid.NewGuid());
        var settings = await matchmakingSettingsProvider.Provide();
        var matchmakingResult =
            Domain.Matchmaking.Matchmaking.Create(newMatchmakingId, AggregateVersion.zero, settings);

        if (!matchmakingResult.IsOk)
            throw new JoiningQuickMatchmakingFailedException(command.Nick,
                JoiningQuickMatchmakingFailReason.ErrorDuringSettingUp);

        var (matchmaking, creationEvents) = matchmakingResult.ResultValue;
        var matchmakingParticipant = matchmakingParticipantFactory.CreateFromNick(command.Nick);
        var matchmakingJoinResult = matchmaking.Join(matchmakingParticipant);

        var matchmakingAndEvents = CreateValueTupleForMatchmakingAndEvents(matchmakingJoinResult);

        return matchmakingJoinResult.IsOk
            ? await SaveJoinAndReturnResult(matchmakingAndEvents, messageContext,
                matchmakingParticipant.Id, creationEvents, ct)
            : throw TranslateDomainError(matchmakingJoinResult.ErrorValue, matchmaking, matchmakingParticipant);
    }

    private static (Matchmaking, IReadOnlyCollection<Event.MatchmakingEventPayload>)
        CreateValueTupleForMatchmakingAndEvents(
            FSharpResult<Tuple<Matchmaking, FSharpList<Event.MatchmakingEventPayload>>, Error>
                matchmakingAndEventsResult)
    {
        return (matchmakingAndEventsResult.ResultValue.Item1,
            matchmakingAndEventsResult.ResultValue.Item2.ToList());
    }

    private async Task<Result> SaveJoinAndReturnResult(
        (Domain.Matchmaking.Matchmaking matchmakingAfterJoin, IReadOnlyCollection<Event.MatchmakingEventPayload> events)
            result,
        MessageContext messageContext,
        Domain.Matchmaking.ParticipantModule.Id participantId,
        CancellationToken ct)
        => await SaveJoinAndReturnResult(result, messageContext, participantId, [], ct);

    private async Task<Result> SaveJoinAndReturnResult(
        (Domain.Matchmaking.Matchmaking matchmakingAfterJoin, IReadOnlyCollection<Event.MatchmakingEventPayload> events)
            result,
        MessageContext messageContext,
        Domain.Matchmaking.ParticipantModule.Id participantId,
        IEnumerable<Event.MatchmakingEventPayload> prependEvents,
        CancellationToken ct)
    {
        var (matchmakingAfterJoin, joinEvents) = result;
        var allEventPayloads = prependEvents.Concat(joinEvents).ToList();
        var expectedVersion = matchmakingAfterJoin.Version_;

        await matchmakings.SaveAsync(
            matchmakingAfterJoin.Id_,
            ListModule.OfSeq(allEventPayloads),
            expectedVersion,
            messageContext.CorrelationId,
            messageContext.CausationId,
            ct
        ).AwaitOrWrap(_ =>
            new JoiningQuickMatchmakingFailedException("<unknown>", JoiningQuickMatchmakingFailReason.Unknown));

        return new Result(matchmakingAfterJoin.Id_, participantId);
    }

    private static System.Exception TranslateDomainError(
        Error error,
        Matchmaking matchmaking,
        Participant matchmakingParticipant) => error switch
    {
        Error.ParticipantAlreadyJoined => new MatchmakingParticipantAlreadyJoinedException(
            matchmaking, matchmakingParticipant),
        Error.RoomFull => new MatchmakingRoomFullException(matchmaking),
        Error.InvalidPhase invalidPhaseError => new JoiningMatchmakingInvalidPhaseException(
            invalidPhaseError.Expected.ToList(), invalidPhaseError.Actual),
        _ => new UnreachableException(error.ToString())
    };
}