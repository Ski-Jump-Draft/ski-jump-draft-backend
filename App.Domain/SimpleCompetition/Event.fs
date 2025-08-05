namespace App.Domain.SimpleCompetition.Event

open App.Domain.SimpleCompetition

// ---------- V1 ----------

type RoundLimitDtoV1 =
    | NoneLimit
    | Soft of Value: int
    | Exact of Value: int * TieBreakerCriteria: string

type RoundSettingsDtoV1 =
    { RoundLimit: RoundLimitDtoV1
      SortStartlist: bool
      ResetPoints: bool
      GroupIndexesToSort: int list option }

type CompetitionSettingsDtoV1 =
    { Rounds: int
      RoundSettings: RoundSettingsDtoV1 list }

type CompetitionHillDtoV1 =
    { Id: Hill.Id
      KPoint: float
      HsPoint: float
      GatePoints: float
      HeadwindPoints: float
      TailwindPoints: float }

type CompetitorDtoV1 =
    { Id: Competitor.Id
      TeamId: Team.Id option }

type TeamCompetitorDtoV1 = { Id: Competitor.Id }

type TeamDtoV1 =
    { Id: Team.Id
      Competitors: TeamCompetitorDtoV1 list }

type JumpDtoV1 =
    { Id: Jump.Id
      CompetitorId: Competitor.Id
      Distance: float
      WindAverage: double
      JudgeNotes: float list }

// Eventy

type IndividualCompetitionCreatedV1 =
    { CompetitionId: CompetitionId
      Settings: CompetitionSettingsDtoV1
      Hill: CompetitionHillDtoV1
      Competitors: CompetitorDtoV1 list
      StartingGate: Jump.Gate }

type TeamCompetitionCreatedV1 =
    { CompetitionId: CompetitionId
      Settings: CompetitionSettingsDtoV1
      Hill: CompetitionHillDtoV1
      Teams: TeamDtoV1 list
      StartingGate: Jump.Gate }

type CompetitionStartedV1 = { CompetitionId: CompetitionId }

type CompetitionRoundStartedV1 =
    { CompetitionId: CompetitionId
      RoundIndex: RoundIndex }

type CompetitionRoundEndedV1 =
    { CompetitionId: CompetitionId
      RoundIndex: RoundIndex }

type CompetitionGroupStartedV1 =
    { CompetitionId: CompetitionId
      RoundIndex: RoundIndex
      GroupIndex: GroupIndex }

type CompetitionGroupEndedV1 =
    { CompetitionId: CompetitionId
      RoundIndex: RoundIndex
      GroupIndex: GroupIndex }

type JumpAddedV1 =
    { CompetitionId: CompetitionId
      Jump: JumpDtoV1 }

type StartingGateSetV1 =
    { CompetitionId: CompetitionId
      Gate: int }

type GateLoweredByCoachV1 =
    { CompetitionId: CompetitionId
      Count: int }

type GateChangedByJuryV1 =
    { CompetitionId: CompetitionId
      Count: int }

type CompetitorDisqualifiedV1 =
    { CompetitionId: CompetitionId
      CompetitorId: Competitor.Id
      Reason: DisqualificationReason }

type CompetitorDidNotStartV1 =
    { CompetitionId: CompetitionId
      CompetitorId: Competitor.Id }

type CompetitionCancelledV1 =
    { CompetitionId: CompetitionId
      Reason: string }

type CompetitionSuspendedV1 =
    { CompetitionId: CompetitionId
      Reason: string }

type CompetitionContinuedV1 = { CompetitionId: CompetitionId }

type CompetitionEndedV1 = { CompetitionId: CompetitionId }

type CompetitionEventPayload =
    | IndividualCompetitionCreatedV1 of IndividualCompetitionCreatedV1
    | TeamCompetitionCreatedV1 of TeamCompetitionCreatedV1
    | CompetitionStartedV1 of CompetitionStartedV1
    | CompetitionGroupStartedV1 of CompetitionGroupStartedV1
    | CompetitionGroupEndedV1 of CompetitionGroupEndedV1
    | CompetitionRoundStartedV1 of CompetitionRoundStartedV1
    | CompetitionRoundEndedV1 of CompetitionRoundEndedV1
    | JumpAddedV1 of JumpAddedV1
    | StartingGateSetV1 of StartingGateSetV1
    | GateLoweredByCoachV1 of GateLoweredByCoachV1
    | GateChangedByJuryV1 of GateChangedByJuryV1
    | CompetitorDisqualifiedV1 of CompetitorDisqualifiedV1
    | CompetitorDidNotStartV1 of CompetitorDidNotStartV1
    | CompetitionCancelledV1 of CompetitionCancelledV1
    | CompetitionSuspendedV1 of CompetitionSuspendedV1
    | CompetitionContinuedV1 of CompetitionContinuedV1
    | CompetitionEndedV1 of CompetitionEndedV1

module Versioning =
    let schemaVersion =
        function
        | IndividualCompetitionCreatedV1 _ -> 1us
        | TeamCompetitionCreatedV1 _ -> 1us
        | CompetitionStartedV1 _ -> 1us
        | CompetitionRoundStartedV1 _ -> 1us
        | CompetitionRoundEndedV1 _ -> 1us
        | CompetitionGroupStartedV1 _ -> 1us
        | CompetitionGroupEndedV1 _ -> 1us
        | JumpAddedV1 _ -> 1us
        | StartingGateSetV1 _ -> 1us
        | GateLoweredByCoachV1 _ -> 1us
        | GateChangedByJuryV1 _ -> 1us
        | CompetitorDisqualifiedV1 _ -> 1us
        | CompetitorDidNotStartV1 _ -> 1us
        | CompetitionCancelledV1 _ -> 1us
        | CompetitionSuspendedV1 _ -> 1us
        | CompetitionContinuedV1 _ -> 1us
        | CompetitionEndedV1 _ -> 1us
