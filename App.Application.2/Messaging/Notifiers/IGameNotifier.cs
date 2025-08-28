namespace App.Application._2.Messaging.Notifiers;

using System;

public interface IGameNotifier
{
    Task GameStartedAfterMatchmaking(Guid matchmakingId, Guid gameId);
    Task GameUpdated(GameUpdatedDto matchmaking);
}

// ───────── Root ─────────

public sealed record GameUpdatedDto(
    Guid GameId,
    int SchemaVersion,
    string Status,                       // "PreDraft" | "Draft" | "MainCompetition" | "Ended" | "Break"
    string ChangeType,                   // "Snapshot" | "PhaseChanged" | "DraftPickMade" | "JumpAdded" | ...
    GameHeaderDto Header,
    PreDraftDto? PreDraft,
    DraftDto? Draft,
    CompetitionDto? MainCompetition,
    BreakDto? Break,
    EndedDto? Ended
);

// ───────── Header (stabilne słowniki referencyjne) ─────────

public sealed record GameHeaderDto(
    Guid? HillId,
    IReadOnlyList<PlayerDto> Players,
    IReadOnlyList<JumperDto> Jumpers
);

public sealed record PlayerDto(Guid PlayerId, string Nick);
public sealed record JumperDto(Guid JumperId);

// ───────── PreDraft ─────────

public sealed record PreDraftDto(
    string Mode,                         // "Running" | "Break"
    int Index,                           // 0-based index aktualnego/polskiego konkursu pre-draft
    CompetitionDto? Competition     // null, jeśli Break
);

// ───────── Draft ─────────

public sealed record DraftDto(
    Guid? CurrentPlayerId,               // null jeśli draft się skończył
    bool Ended,
    IReadOnlyList<PlayerPicksDto> Picks  // Picks per gracz
);

public sealed record PlayerPicksDto(Guid PlayerId, IReadOnlyList<Guid> JumperIds);

// ───────── Competition (lekki widok) ─────────

public sealed record CompetitionDto(
    string Status,                       // "NotStarted" | "RoundInProgress" | "Suspended" | "Cancelled" | "Ended"
    Guid? NextJumperId,                  // kolejny na liście startowej (bez BIB – domena nie wystawia)
    GateDto Gate                         // aktualny stan belki
);

public sealed record GateDto(
    int Starting,
    int CurrentJury,
    int? CoachReduction                  // >=1 gdy trener obniżył; null gdy brak
);

// ───────── Break / Ended ─────────

public sealed record BreakDto(string Next); // "PreDraft" | "Draft" | "MainCompetition" | "Ended"

public sealed record EndedDto(
    string Policy                         // "Classic" | "PodiumAtAllCosts" (opcjonalnie rozwiniesz później)
);

