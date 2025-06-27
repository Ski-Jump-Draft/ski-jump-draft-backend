namespace App.Domain.PreDraft

open App.Domain
open App.Domain.Shared.EventHelpers
open App.Domain.PreDraft.Competitions
open App.Domain.Time

module PreDraft =
    [<Struct>]
    type Id = Id of System.Guid

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

    type Event =
        | PreDraftStarted of PreDraftId: Id * Timestamp: EventTimestamp
        | CompetitionStarted of
            PreDraftId: Id *
            Timestamp: EventTimestamp *
            Index: CompetitionIndex *
            CompetitionId: Competition.Id
        | CompetitionEnded of
            PreDraftId: Id *
            Timestamp: EventTimestamp *
            Index: CompetitionIndex *
            CompetitionId: Competition.Id
        | PreDraftEnded of PreDraftId: Id * Timestamp: EventTimestamp

    type Error = InvalidPhase of Expected: PhaseTag list * Actual: PhaseTag

open PreDraft

type PreDraft =
    { Id: PreDraft.Id
      Phase: PreDraft.Phase
      Settings: PreDraft.Settings.Settings
      Clock: IClock }

    static member TagOfPhase phase =
        match phase with
        | NotStarted -> NotStartedTag
        | Competition _ -> CompetitionTag
        | Break _ -> BreakTag
        | Ended -> EndedTag

    static member Create id settings clock =
        { Id = id
          Phase = PreDraft.NotStarted
          Settings = settings
          Clock = clock }

    member this.StartCompetition(competitionId: Competition.Id) =
        match this.Phase with
        | NotStarted ->
            let index = CompetitionIndex(0u)

            let state =
                { this with
                    Phase = Phase.Competition(index, competitionId) }

            let preDraftStartedEvent = Event.PreDraftStarted(this.Id, this.Clock.UtcNow)

            let competitionStartedEvent =
                Event.CompetitionStarted(this.Id, this.Clock.UtcNow, index, competitionId)

            Ok(state, [ preDraftStartedEvent; competitionStartedEvent ])
        | Break nextCompetitionIndex ->
            let state =
                { this with
                    Phase = Competition(nextCompetitionIndex, competitionId) }

            let event =
                Event.CompetitionStarted(this.Id, this.Clock.UtcNow, nextCompetitionIndex, competitionId)

            Ok(state, [ event ])
        | _ -> Error(InvalidPhase([ PhaseTag.NotStartedTag; PhaseTag.BreakTag ], PreDraft.TagOfPhase(this.Phase)))

    member this.EndCurrentCompetition =
        match this.Phase with
        | Competition(competitionIndex, competitionId) ->
            let (CompetitionIndex competitionIndexUint) = competitionIndex
            let maxCompetitionIndex = uint (this.Settings.Competitions.Length - 1)
            let nextCompetitionIndexUint = competitionIndexUint + 1u
            let shouldEnd = nextCompetitionIndexUint > maxCompetitionIndex

            let phase =
                if shouldEnd then
                    Phase.Ended
                else
                    Phase.Break(CompetitionIndex(nextCompetitionIndexUint))

            let state = { this with Phase = phase }

            let competitionEndedEvent =
                Event.CompetitionEnded(this.Id, this.Clock.UtcNow, competitionIndex, competitionId)

            let preDraftEnded = Event.PreDraftEnded(this.Id, this.Clock.UtcNow)

            let events =
                if shouldEnd then
                    [ competitionEndedEvent; preDraftEnded ]
                else
                    [ competitionEndedEvent ]

            Ok(state, events)
        | _ -> Error(InvalidPhase([ PhaseTag.CompetitionTag ], PreDraft.TagOfPhase(this.Phase)))
