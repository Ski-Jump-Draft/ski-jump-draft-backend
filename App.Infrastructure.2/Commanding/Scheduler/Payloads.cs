namespace App.Infrastructure._2.Commanding.Scheduler;

public record EndMatchmakingPayload(Guid MatchmakingId);
public record StartGamePayload(Guid MatchmakingId);
public record StartPreDraftPayload(Guid GameId);
