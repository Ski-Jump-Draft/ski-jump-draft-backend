using App.Application.Commanding;
using App.Application.Projection;
using App.Domain.Matchmaking;
using App.Domain.Shared;

namespace App.Web.Sse.Notifier;

public class MatchmakingNotifierByDomainEvents(
    ISseStream sse,
    IActiveMatchmakingsProjection activeMatchmakings)
    : IEventHandler<Event.MatchmakingEventPayload>
{
    public async Task HandleAsync(DomainEvent<Event.MatchmakingEventPayload> @event, CancellationToken ct)
    {
        switch (@event.Payload)
        {
            case Event.MatchmakingEventPayload.MatchmakingParticipantJoinedV1 playerJoinedEvent:
            {
                var matchmakingId = playerJoinedEvent.Item.MatchmakingId.Item;
                var matchmaking = await activeMatchmakings.GetActiveMatchmakingAsync(matchmakingId, ct);
                ValidateActiveMatchmaking(matchmaking, matchmakingId, "MatchmakingParticipantJoinedV1");
                await sse.PublishAsync(matchmakingId.ToString(), "updated", new
                {
                    CurrentPlayersCount = matchmaking!.CurrentPlayersCount,
                    MaxPlayersCount = matchmaking.MaxPlayersCount
                }, ct);
                break;
            }
            case Event.MatchmakingEventPayload.MatchmakingParticipantLeftV1 playerLeftEvent:
            {
                var matchmakingId = playerLeftEvent.Item.MatchmakingId.Item;
                var matchmaking = await activeMatchmakings.GetActiveMatchmakingAsync(matchmakingId, ct);
                ValidateActiveMatchmaking(matchmaking, matchmakingId, "MatchmakingParticipantLeftV1");
                await sse.PublishAsync(matchmakingId.ToString(), "updated", new
                {
                    CurrentPlayersCount = matchmaking!.CurrentPlayersCount,
                    MaxPlayersCount = matchmaking.MaxPlayersCount
                }, ct);
                break;
            }
            case Event.MatchmakingEventPayload.MatchmakingEndedV1 matchmakingEndedEvent:
            {
                var matchmakingId = matchmakingEndedEvent.Item.MatchmakingId.Item;

                await sse.PublishAsync(matchmakingId.ToString(), "ended", new
                {
                    PlayersCount = matchmakingEndedEvent.Item,
                }, ct);
                break;
            }
            case Event.MatchmakingEventPayload.MatchmakingFailedV1 matchmakingFailedEvent:
            {
                var matchmakingId = matchmakingFailedEvent.Item.MatchmakingId.Item;

                await sse.PublishAsync(matchmakingId.ToString(), "failed", new
                {
                    PlayersCount = matchmakingFailedEvent.Item.ParticipantsCount,
                    Reason = matchmakingFailedEvent.Item.Reason.ToString(),
                }, ct);
                break;
            }
        }
    }

    private static void ValidateActiveMatchmaking(ActiveMatchmakingDto? matchmaking, Guid matchmakingId,
        string eventName)
    {
        if (matchmaking is null)
        {
            throw new NullReferenceException($"No active matchmaking found for matchmakingId: {matchmakingId} despite {
                eventName}");
        }
    }
}