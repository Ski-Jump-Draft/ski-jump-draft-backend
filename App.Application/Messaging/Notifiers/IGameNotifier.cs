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
    EndedPreDraftDto? EndedPreDraft,
    DraftDto? Draft,
    CompetitionDto? MainCompetition,
    BreakDto? Break,
    EndedDto? Ended,
    CompetitionDto? LastCompetitionState,
    CompetitionRoundResultDto? LastCompetitionResultDto
);

// ───────── Header (stabilne słowniki referencyjne) ─────────

public sealed record GameHeaderDto(
    string DraftOrderPolicy,
    int? DraftTimeoutInSeconds,
    GameHillDto Hill,
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

public sealed record GameHillDto(
    Guid GameHillId,
    Guid GameWorldHillId,
    string Name,
    string Location,
    double K,
    double Hs,
    string CountryFisCode,
    string Alpha2Code
);

// ───────── Next Status ─────────

public sealed record NextStatusDto(string Status, TimeSpan In);

// ───────── PreDraft ─────────
public sealed record EndedPreDraftDto(
    List<EndedCompetitionResults> EndedCompetitions);

public sealed record PreDraftDto(
    string Status, // "Running" | "Break"
    int? Index, // 0-based index aktualnego/polskiego konkursu pre-draft
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
    int? RoundIndex,
    IReadOnlyList<StartlistJumperDto> Startlist,
    GateStateDto GateState,
    IReadOnlyList<CompetitionResultDto> Results,
    int? NextJumpInMilliseconds
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

public sealed record EndedCompetitionResults(
    IReadOnlyList<CompetitionResultDto> Results);

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

public sealed record GateStateDto(
    int Starting,
    int CurrentJury,
    int? CoachReduction
);

// ───────── Break / Ended ─────────

public sealed record BreakDto(string Next); // "PreDraft" | "Draft" | "MainCompetition" | "Ended"

public sealed record EndedDto(
    string Policy, // Classic | PodiumAtAllCosts
    Dictionary<Guid, PositionAndPoints> Ranking // position, points
);

public sealed record PositionAndPoints(int Position, int Points);