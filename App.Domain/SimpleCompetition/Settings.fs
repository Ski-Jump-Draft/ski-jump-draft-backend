namespace App.Domain.SimpleCompetition

/// Settings of a group in a team ski jumping competition.
///
/// Group is a mini-round where n-th jumper of every team jumps
type GroupSettings =
    {
        /// Indicates during which groups startlist should be sorted, according to the indexes
        GroupIndexesToSort: Set<GroupIndex>
    }

/// Represents a criteria using to determine who should advance, when RoundLimit is set to Exact and ex aequos at the last position are present.
type TieBreakerCriteria =
    | LongestJump
    | BestJudgePoints
    | HighestBib
    | LowestBib
    | Random // TODO

type RoundLimitValue = private RoundLimitValue of int

module RoundLimitValue =
    type Error = | BelowTwo

    let tryCreate (v: int) =
        if v < 2 then
            Error(Error.BelowTwo)
        else
            Ok(RoundLimitValue v)

type RoundLimit =
    | NoneLimit
    | Soft of Value: RoundLimitValue
    | Exact of Value: RoundLimitValue * TieBreakerCriteria: TieBreakerCriteria

type RoundSettings =
    { RoundLimit: RoundLimit
      SortStartlist: bool
      ResetPoints: bool
      GroupSettings: GroupSettings option }

module Settings =
    type Error = | RoundSettingsEmpty

type Settings =
    private
        { RoundSettings: RoundSettings list }

    static member Create(roundSettings: RoundSettings list) =
        if roundSettings.IsEmpty then
            Error(Settings.Error.RoundSettingsEmpty)
        else
            Ok { RoundSettings = roundSettings }
