namespace App.Application.Messaging.Notifiers;

using System;

public interface IGameNotifier
{
    Task GameStartedAfterMatchmaking(Guid matchmakingId, Guid gameId,
        Dictionary<Guid, Guid> playersMapping);

    Task GameUpdated(GameUpdatedDto matchmaking);
    Task GameEnded(Guid gameId);
}

// ───────── Root ─────────

public sealed record GameUpdatedDto(
    Guid GameId,
    int SchemaVersion,
    string Status, // "PreDraft" | "Draft" | "MainCompetition" | "Ended" | "Break"
    NextStatusDto? NextStatus,
    string ChangeType, // "Snapshot" | "PhaseChanged" | "DraftPickMade" | "JumpAdded" | ...
    int PreDraftsCount,
    GameHeaderDto Header,
    PreDraftDto? PreDraft,
    DraftDto? Draft,
    CompetitionDto? MainCompetition,
    BreakDto? Break,
    EndedDto? Ended,
    CompetitionDto? LastCompetitionState
);

// ───────── Header (stabilne słowniki referencyjne) ─────────

public sealed record GameHeaderDto(
    Guid? HillId,
    IReadOnlyList<PlayerDto> Players,
    IReadOnlyList<JumperDto> Jumpers
);

public sealed record PlayerDto(Guid PlayerId, string Nick);

public sealed record JumperDto(Guid JumperId);

// ───────── Next Status ─────────

public sealed record NextStatusDto(string Status, TimeSpan In);

// ───────── PreDraft ─────────

public sealed record PreDraftDto(
    string Mode, // "Running" | "Break"
    int Index, // 0-based index aktualnego/polskiego konkursu pre-draft
    CompetitionDto? Competition // null, jeśli Break
);

// ───────── Draft ─────────

public sealed record DraftDto(
    Guid? CurrentPlayerId,
    int? TimeoutInSeconds,
    bool Ended,
    string OrderPolicy, // Classic | Snake | Random
    IReadOnlyList<PlayerPicksDto> Picks,
    IReadOnlyList<DraftPickOptionDto> AvailableJumpers,
    IReadOnlyList<Guid> NextPlayers);

public sealed record DraftPickOptionDto(
    Guid GameJumperId,
    string Name,
    string Surname,
    string CountryFisCode,
    IEnumerable<int> TrainingRanks);

public sealed record PlayerPicksDto(Guid PlayerId, IReadOnlyList<Guid> JumperIds);

// ───────── Competition (lekki widok) ─────────

public sealed record CompetitionDto(
    string Status, // "NotStarted" | "RoundInProgress" | "Suspended" | "Cancelled" | "Ended"
    Guid? NextJumperId,
    GateDto Gate,
    IReadOnlyList<CompetitionResultDto> Results
);

public sealed record CompetitionResultDto(
    double Rank,
    int Bib,
    CompetitionJumperDto Jumper,
    double Total,
    IReadOnlyList<CompetitionRoundResultDto> Rounds
);

public sealed record CompetitionRoundResultDto(
    double Distance,
    double Points,
    double? JudgePoints,
    double? WindPoints,
    double? WindAverage
);

public sealed record CompetitionJumperDto(
    Guid Id,
    string Name,
    string Surname,
    string CountryFisCode
);

public sealed record GateDto(
    int Starting,
    int CurrentJury,
    int? CoachReduction
);

// ───────── Break / Ended ─────────

public sealed record BreakDto(string Next); // "PreDraft" | "Draft" | "MainCompetition" | "Ended"

public sealed record EndedDto(
    string Policy, // Classic | PodiumAtAllCosts
    Dictionary<Guid, (int, int)> Ranking // position, points
);