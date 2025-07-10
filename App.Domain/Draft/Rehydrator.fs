module App.Domain.Draft.Rehydrator

open App.Domain.Draft.Event
open App.Domain.Draft.Order
open App.Domain.Shared
open App.Domain.Shared.EventHelpers

let private initialEmpty draftId =
    Draft.Create
        draftId
        (Unchecked.defaultof<Settings.Settings>) // nadpisze pierwszy event
        []
        0UL // nadpisze pierwszy event

let rehydrate (draftId: Id.Id) (events: DomainEvent<DraftEventPayload> seq) : Draft =
    let seedState = initialEmpty draftId

    events
    |> Seq.fold
        (fun state event ->
            match event.Payload with

            | DraftEventPayload.DraftStartedV1 payload ->
                let strategy = OrderStrategyFactory.create payload.Settings.Order
                let initialOrd = strategy.ComputeInitialOrder(payload.Participants, payload.Seed)

                { state with
                    Settings = payload.Settings
                    Participants = payload.Participants
                    Seed = payload.Seed
                    Phase = Running(0, initialOrd, Picks.Picks.Empty initialOrd) }

            | DraftEventPayload.DraftSubjectPickedV2 x ->
                match state.Phase with
                | Running(idx, ord, picks) ->
                    let picks' = picks.AddPick(x.ParticipantId, x.SubjectId)
                    let total = picks'.Total()
                    let count = List.length ord
                    let rounds = total / count
                    let strategy = OrderStrategyFactory.create state.Settings.Order
                    let (nextOrd, nextIdx) = strategy.ComputeNextOrder(ord, idx, rounds, state.Seed)

                    let newPhase =
                        if total = int state.Settings.MaxJumpersPerPlayer * count then
                            Done picks'
                        else
                            Running(nextIdx, nextOrd, picks')

                    { state with Phase = newPhase }
                | _ -> state

            | DraftEventPayload.DraftEndedV1 _ ->
                match state.Phase with
                | Running(_, _, picks) -> { state with Phase = Done picks }
                | _ -> state

            | _ -> state)
        seedState
