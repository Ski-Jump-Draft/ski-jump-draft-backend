namespace App.Infrastructure.Commanding.Scheduler;

public record EndMatchmakingPayload(Guid MatchmakingId);
public record StartGamePayload(Guid MatchmakingId);
public record StartPreDraftPayload(Guid GameId);
public record SimulateJumpInGamePayload(Guid GameId);
public record StartNextPreDraftCompetitionPayload(Guid GameId);
public record StartDraftPayload(Guid GameId);
public record StartMainCompetitionPayload(Guid GameId);
public record PickJumperPayload(Guid GameId, Guid PlayerId, Guid JumperId);
public record PassPickPayload(Guid GameId, Guid PlayerId, int TurnIndex);
public record PickByBot(Guid GameId, Guid PlayerId);
public record EndGamePayload(Guid GameId);
