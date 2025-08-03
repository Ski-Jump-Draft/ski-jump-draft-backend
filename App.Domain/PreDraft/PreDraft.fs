namespace App.Domain.PreDraft

open App.Domain
open App.Domain.PreDraft.Event

open App.Domain.PreDraft.Phase

module PreDraft =
    type Error = InvalidPhase of Expected: PhaseTag list * Actual: PhaseTag

open PreDraft

type PreDraft =
    { Id: Id.Id
      Phase: Phase
      Settings: PreDraft.Settings.Settings }

    static member TagOfPhase phase =
        match phase with
        | InProgress _ -> InProgressTag
        | Ended -> EndedTag

    static member Create (id: Id.Id) settings firstCompetitionId : Result<PreDraft * PreDraftEventPayload list, Error> =
        let state =
            { Id = id
              Phase = InProgress(CompetitionIndex 0u, firstCompetitionId)
              Settings = settings }

        let createdEvent: PreDraftCreatedV1 = { PreDraftId = id; Settings = settings }

        let preDraftCompetitionStarted: PreDraftCompetitionStartedV1 =
            { PreDraftId = id
              Index = CompetitionIndex 0u
              CompetitionId = firstCompetitionId }

        Ok(
            state,
            [ PreDraftEventPayload.PreDraftCreatedV1 createdEvent
              PreDraftEventPayload.PreDraftCompetitionStartedV1 preDraftCompetitionStarted ]
        )

    member this.Advance(maybeCompetitionId: Competition.Id option) =
        match this.Phase with
        | InProgress(index, competitionId) ->
            match maybeCompetitionId with
            | None ->
                let state = { this with Phase = Ended }
                let event = PreDraftEndedV1 { PreDraftId = this.Id }
                Ok(state, [ event ])

            | Some nextCompetitionId ->
                let nextCompetitionIndex =
                    let (CompetitionIndex i) = index in CompetitionIndex(i + 1u) // lol

                let state =
                    { this with
                        Phase = InProgress(nextCompetitionIndex, nextCompetitionId) }

                let event =
                    PreDraftCompetitionStartedV1
                        { PreDraftId = this.Id
                          CompetitionId = nextCompetitionId
                          Index = nextCompetitionIndex }

                Ok(state, [ event ])

        | Ended -> Error(InvalidPhase([ PhaseTag.InProgressTag ], PreDraft.TagOfPhase(this.Phase)))
