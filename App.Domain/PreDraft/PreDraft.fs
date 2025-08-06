namespace App.Domain.PreDraft

open App.Domain
open App.Domain.PreDraft.Event

open App.Domain.PreDraft.Phase
open App.Domain.Shared.AggregateVersion

module PreDraft =
    type Error =
        | InvalidPhase of Expected: PhaseTag list * Actual: PhaseTag
        | TooFewCompetitionsPlayedForEnd of CurrentIndex: CompetitionIndex * CompetitionsCount: int

open PreDraft

type PreDraft =
    private
        { Id: Id.Id
          Version: AggregateVersion
          Phase: Phase
          Settings: PreDraft.Settings.Settings }

    member this.Id_ = this.Id
    member this.Version_ = this.Version
    member this.Phase_ = this.Phase
    member this.Settings_ = this.Settings

    static member TagOfPhase phase =
        match phase with
        | InProgress _ -> InProgressTag
        | Ended -> EndedTag

    static member Create
        (id: Id.Id)
        version
        settings
        firstCompetitionId
        : Result<PreDraft * PreDraftEventPayload list, Error> =
        let state =
            { Id = id
              Version = version
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

    member this.Advance(nextCompetitionId) =
        match this.Phase with
        | InProgress(index, competitionId) ->
            let nextCompetitionIndex =
                let (CompetitionIndex i) = index in CompetitionIndex(i + 1u) // lol

            let state =
                { this with
                    Phase = InProgress(nextCompetitionIndex, nextCompetitionId); Version = increment this.Version }

            let event =
                PreDraftCompetitionStartedV1
                    { PreDraftId = this.Id
                      CompetitionId = nextCompetitionId
                      Index = nextCompetitionIndex }

            Ok(state, [ event ])

        | Ended -> Error(InvalidPhase([ PhaseTag.InProgressTag ], PreDraft.TagOfPhase(this.Phase)))

    member this.End =
        match this.Phase with
        | InProgress(index, _) ->
            let (CompetitionIndex intIndex) = index

            if int (intIndex) + 1 = this.Settings.CompetitionsCount then
                let state = { this with Phase = Ended; Version = increment this.Version }
                let event = { PreDraftId = this.Id }: PreDraftEndedV1
                Ok(state, [ PreDraftEventPayload.PreDraftEndedV1 event ])
            else
                Error(Error.TooFewCompetitionsPlayedForEnd(index, this.Settings.CompetitionsCount))
        | Ended -> Error(InvalidPhase([ PhaseTag.InProgressTag ], PreDraft.TagOfPhase(this.Phase)))
