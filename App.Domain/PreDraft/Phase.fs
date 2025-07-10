module App.Domain.PreDraft.Phase

open App.Domain.PreDraft.Competitions

[<Struct>]
type CompetitionIndex = CompetitionIndex of uint

type Phase =
    | NotStarted
    | Competition of Index: CompetitionIndex * CompetitionId: Competition.Id
    | Break of NextIndex: CompetitionIndex
    | Ended

type PhaseTag =
    | NotStartedTag
    | CompetitionTag
    | BreakTag
    | EndedTag
