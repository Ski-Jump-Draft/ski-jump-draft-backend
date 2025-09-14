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
    CompetitionDto? LastCompetitionState,
    CompetitionRoundResultDto? LastCompetitionResultDto
);

// ───────── Header (stabilne słowniki referencyjne) ─────────

public sealed record GameHeaderDto(
    Guid? HillId,
    IReadOnlyList<GamePlayerDto> Players,
    IReadOnlyList<GameJumperDto> Jumpers,
    IReadOnlyList<CompetitionJumperDto> CompetitionJumpers);

public sealed record GamePlayerDto(Guid PlayerId, string Nick, bool IsBot);

public sealed record GameJumperDto(
    Guid GameJumperId,
    Guid GameWorldJumperId,
    string Name,
    string Surname,
    string CountryFisCode);

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
    IReadOnlyList<StartlistJumperDto> Startlist,
    GateDto Gate,
    IReadOnlyList<CompetitionResultDto> Results,
    int? NextJumpInSeconds
)
{
    public Guid? NextJumperId
    {
        get
        {
            var id = Startlist.FirstOrDefault(startlistJumper => !startlistJumper.Done)?.CompetitionJumperId;
            return id;
        }
    }
};

public sealed record StartlistJumperDto(
    int Bib,
    bool Done,
    Guid CompetitionJumperId
);

public sealed record CompetitionResultDto(
    double Rank,
    int Bib,
    Guid CompetitionJumperId,
    double Total,
    IReadOnlyList<CompetitionRoundResultDto> Rounds
);

public sealed record CompetitionRoundResultDto(
    Guid GameJumperId,
    Guid CompetitionJumperId,
    double Distance,
    double Points,
    IReadOnlyList<double>? Judges,
    double? JudgePoints,
    double? WindCompensation,
    double WindAverage,
    double? GateCompensation,
    double? TotalCompensation
);

public sealed record CompetitionJumperDto(
    Guid GameJumperId,
    Guid CompetitionJumperId,
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