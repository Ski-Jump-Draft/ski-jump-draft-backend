using App.Application.Commanding;
using App.Application.Exception;
using App.Application.Ext;
using App.Application.UseCase.Game.Exception;
using App.Application.UseCase.Helper;
using App.Domain.Matchmaking;
using App.Domain.Shared;
using App.Domain.Repositories;

namespace App.Application.UseCase.Game.QuickGame.JoinMatchmaking;

// TODO: Walidacja i nie raw string!
public record Command(
    Guid MatchmakingId,
    string Nick
) : ICommand<App.Domain.Matchmaking.ParticipantModule.Id>;

public class Handler(
    IMatchmakingRepository matchmakings,
    IMatchmakingParticipantRepository matchmakingParticipants,
    IMatchmakingParticipantFactory matchmakingParticipantFactory,
    IGuid guid
) : ICommandHandler<Command, App.Domain.Matchmaking.ParticipantModule.Id>
{
    public async Task<App.Domain.Matchmaking.ParticipantModule.Id> HandleAsync(Command command, CancellationToken ct)
    {
        var matchmakingId = Id.NewId(command.MatchmakingId);
        var matchmaking = await matchmakings.LoadAsync(matchmakingId, ct)
            .AwaitOrWrap(_ => new IdNotFoundException<Guid>(matchmakingId.Item));

        var matchmakingParticipant = matchmakingParticipantFactory.CreateFromNick(command.Nick);
        var joinResult = matchmaking.Join(matchmakingParticipant.Id);

        if (joinResult.IsOk)
        {
            var (aggregate, events) = joinResult.ResultValue;

            var correlationId = guid.NewGuid();
            var causationId = correlationId;

            var expectedVersion = aggregate.Version_;

            await
                matchmakings.SaveAsync(aggregate, events, expectedVersion, correlationId, causationId, ct)
                    .AwaitOrWrap(_ =>
                        new JoiningQuickMatchmakingFailedException(command.Nick,
                            JoiningQuickMatchmakingFailReason.Unknown));

            await matchmakingParticipants.SaveAsync(matchmakingParticipant.Id, matchmakingParticipant).AwaitOrWrap(_ =>
                new JoiningQuickMatchmakingFailedException(command.Nick,
                    JoiningQuickMatchmakingFailReason.ErrorDuringPreservingParticipant));
            return matchmakingParticipant.Id;
        }

        var error = joinResult.ErrorValue;

        throw error switch
        {
            Error.PlayerAlreadyJoined => new MatchmakingParticipantAlreadyJoinedException(matchmaking,
                matchmakingParticipant),
            Error.PlayerNotJoined => new MatchmakingParticipantNotInMatchmakingException(matchmaking,
                matchmakingParticipant),
            Error.RoomFull => new MatchmakingRoomFullException(matchmaking),
            Error.InvalidPhase invalidPhaseError => new JoiningMatchmakingInvalidPhaseException(
                invalidPhaseError.Expected.ToList(), invalidPhaseError.Actual),
            _ => new JoiningQuickMatchmakingFailedException(command.Nick,
                JoiningQuickMatchmakingFailReason.NoServerAvailable)
        };
    }
}