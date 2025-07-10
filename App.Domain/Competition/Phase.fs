module App.Domain.Competition.Phase

//type RoundIndex = RoundIndex of uint

type RoundIndex = RoundIndex of uint
type SubroundIndex = SubroundIndex of uint

type Subround = Subround of RoundIndex: RoundIndex * SubroundIndex: SubroundIndex

type Phase =
    | NotStarted
    | Running of RoundIndex: RoundIndex
    | Break of NextRoundIndex: RoundIndex
    | Suspended of PreviousPhase: Phase
    | Cancelled
    | Ended

type PhaseTag =
    | NotStartedTag
    | RunningTag
    | BreakTag
    | SuspendedTag
    | CancelledTag
    | EndedTag
