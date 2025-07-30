module App.Domain.PreDraft.Phase

open App.Domain.PreDraft.Competitions

[<Struct>]
type CompetitionIndex = CompetitionIndex of uint

// type Phase =
//     | NotStarted
//     | Competition of Index: CompetitionIndex * CompetitionId: Competition.Id
//     | Break of NextIndex: CompetitionIndex
//     | Ended
//
// type PhaseTag =
//     | NotStartedTag
//     | CompetitionTag
//     | BreakTag
//     | EndedTag

type Phase =
    | InProgress of Index: CompetitionIndex * Competition.Id //CompetitionId: Competition.Id
    //| Competition of Index: CompetitionIndex * CompetitionId: Competition.Id
    //| Break of NextIndex: CompetitionIndex
    | Ended

type PhaseTag =
    | InProgressTag
    | EndedTag
