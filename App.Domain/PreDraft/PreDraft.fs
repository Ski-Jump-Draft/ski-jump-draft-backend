namespace App.Domain.PreDraft

open App.Domain
open App.Domain.PreDraft.Event
open App.Domain.PreDraft.Id
open App.Domain.PreDraft.Competitions
open App.Domain.Time

open App.Domain.PreDraft.Phase

module PreDraft =
    type Error = InvalidPhase of Expected: PhaseTag list * Actual: PhaseTag

open PreDraft

type PreDraft =
    { Id: Id
      Phase: Phase
      Settings: PreDraft.Settings.Settings }

    static member TagOfPhase phase =
        match phase with
        | NotStarted -> NotStartedTag
        | Competition _ -> CompetitionTag
        | Break _ -> BreakTag
        | Ended -> EndedTag

    static member Create id settings clock =
        let state =
            { Id = id
              Phase = NotStarted
              Settings = settings }

        let event: PreDraftCreatedV1 = { PreDraftId = id; Settings = settings }

        Ok(state, [ event ])

    member this.StartCompetition(competitionId: Competition.Id) =
        match this.Phase with
        | NotStarted ->
            let index = CompetitionIndex(0u)

            let state =
                { this with
                    Phase = Phase.Competition(index, competitionId) }

            let preDraftStartedEvent: PreDraftStartedV1 = { PreDraftId = this.Id }

            let competitionStartedEvent: CompetitionStartedV1 =
                { PreDraftId = this.Id
                  Index = index
                  CompetitionId = competitionId }

            Ok(
                state,
                [ PreDraftEventPayload.PreDraftStartedV1 preDraftStartedEvent
                  PreDraftEventPayload.CompetitionStartedV1 competitionStartedEvent ]
            )
        | Break nextCompetitionIndex ->
            let state =
                { this with
                    Phase = Competition(nextCompetitionIndex, competitionId) }

            let competitionStartedEvent: CompetitionStartedV1 =
                { PreDraftId = this.Id
                  Index = nextCompetitionIndex
                  CompetitionId = competitionId }

            Ok(state, [ PreDraftEventPayload.CompetitionStartedV1 competitionStartedEvent ])
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

            let competitionEndedEvent: CompetitionEndedV1 =
                { PreDraftId = this.Id
                  Index = competitionIndex
                  CompetitionId = competitionId }

            let preDraftEnded: PreDraftEndedV1 = { PreDraftId = this.Id }

            let events =
                if shouldEnd then
                    [ PreDraftEventPayload.CompetitionEndedV1 competitionEndedEvent
                      PreDraftEventPayload.PreDraftEndedV1 preDraftEnded ]
                else
                    [ PreDraftEventPayload.CompetitionEndedV1 competitionEndedEvent ]

            Ok(state, events)
        | _ -> Error(InvalidPhase([ PhaseTag.CompetitionTag ], PreDraft.TagOfPhase(this.Phase)))
