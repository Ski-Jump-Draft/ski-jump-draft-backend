namespace App.Domain.Draft

open App.Domain.Draft.Event
open App.Domain.Draft.Picks
open App.Domain.Draft.Order
open App.Domain.Draft.Settings
open App.Domain.Shared.AggregateVersion

type PhaseTag =
    | NotStartedTag
    | RunningTag
    | DoneTag

type Phase =
    | NotStarted
    | Running of CurrentIndex: int * ParticipantOrder: Participant.Id list * Picks: Picks
    | Done of Picks

type Error =
    | InvalidPhase of expected: PhaseTag list * actual: PhaseTag
    | ParticipantNotAllowedToPick of Participant.Id
    | PickingLimitReached
    | JumperTaken

type Draft =
    { Id: Id.Id
      Version: AggregateVersion
      Settings: Settings.Settings
      Participants: Participant.Id list
      Seed: uint64
      Phase: Phase }

    static member TagOfPhase =
        function
        | NotStarted -> NotStartedTag
        | Running _ -> RunningTag
        | Done _ -> DoneTag

    static member Create id version settings participants seed =
        let state =
            { Id = id
              Version = version
              Settings = settings
              Participants = participants
              Seed = seed
              Phase = NotStarted }

        let event: DraftCreatedV1 =
            { DraftId = id
              Settings = settings
              Participants = participants
              Seed = seed }

        Ok(state, [ event ])

    member this.Start() =
        match this.Phase with
        | NotStarted ->
            let strategy = OrderStrategyFactory.create this.Settings.Order
            let initialOrd = strategy.ComputeInitialOrder(this.Participants, this.Seed)

            let newState =
                { this with
                    Phase = Running(0, initialOrd, Picks.Empty initialOrd) }

            let payload: DraftStartedV1 = { DraftId = this.Id }

            Ok(newState, [ DraftEventPayload.DraftStartedV1 payload ])

        | _ -> Error(InvalidPhase([ NotStartedTag ], Draft.TagOfPhase this.Phase))

    member this.Pick(participantId: Participant.Id, subjectId: Subject.Id) =
        match this.Phase with
        | Running(currentIdx, order, picks) ->
            if order.[currentIdx] <> participantId then
                Error(ParticipantNotAllowedToPick participantId)
            elif picks.PicksNumberOf participantId >= int this.Settings.MaxJumpersPerPlayer then
                Error PickingLimitReached
            elif this.Settings.UniqueJumpers && picks.ContainsSubject subjectId then
                Error JumperTaken
            else
                let updatedPicks = picks.AddPick(participantId, subjectId)
                let totalAllowed = int this.Settings.MaxJumpersPerPlayer * List.length order
                let completedRounds = updatedPicks.Total() / List.length order

                let pickPayload: DraftSubjectPickedV2 =
                    { DraftId = this.Id
                      ParticipantId = participantId
                      SubjectId = subjectId
                      PickIndex = uint (updatedPicks.PicksNumberOf participantId - 1) }

                let pickEvent = DraftEventPayload.DraftSubjectPickedV2 pickPayload

                let endEvents =
                    if updatedPicks.Total() = totalAllowed then
                        [ DraftEventPayload.DraftEndedV1 { DraftId = this.Id } ]
                    else
                        []

                let strategy = OrderStrategyFactory.create this.Settings.Order

                let (nextOrder, nextIdx) =
                    strategy.ComputeNextOrder(order, currentIdx, completedRounds, this.Seed)

                let newPhase =
                    if updatedPicks.Total() = totalAllowed then
                        Done updatedPicks
                    else
                        Running(nextIdx, nextOrder, updatedPicks)

                let newState = { this with Phase = newPhase }
                Ok(newState, pickEvent :: endEvents)

        | _ -> Error(InvalidPhase([ RunningTag ], Draft.TagOfPhase this.Phase))
