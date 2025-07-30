module App.Domain.Draft.Evolve

open App.Domain.Draft.Event
open App.Domain.Shared
open App.Domain.Shared.AggregateVersion

let evolve (state: Draft) (event: DomainEvent<DraftEventPayload>) =
    let version = AggregateVersion event.Header.AggregateVersion
    match event.Payload with
    | DraftEventPayload.DraftCreatedV1 e ->
        { Id = e.DraftId
          Version = version
          Settings = e.Settings
          Participants = e.Participants
          Seed = e.Seed
          Phase = NotStarted }

    | DraftEventPayload.DraftStartedV1 _ ->
        let strategy = Order.OrderStrategyFactory.create state.Settings.Order
        let initialOrder = strategy.ComputeInitialOrder(state.Participants, state.Seed)
        let picks = Picks.Picks.Empty initialOrder
        { state with Phase = Running(0, initialOrder, picks); Version = version }

    | DraftEventPayload.DraftSubjectPickedV1 _ -> state

    | DraftEventPayload.DraftSubjectPickedV2 e ->
        match state.Phase with
        | Running(currentIdx, order, picks) ->
            let updatedPicks = picks.AddPick(e.ParticipantId, e.SubjectId)
            let totalAllowed = List.length order * int state.Settings.MaxJumpersPerPlayer
            let completedRounds = updatedPicks.Total() / List.length order
            let strategy = Order.OrderStrategyFactory.create state.Settings.Order
            let (nextOrder, nextIdx) = strategy.ComputeNextOrder(order, currentIdx, completedRounds, state.Seed)
            let newPhase =
                if updatedPicks.Total() = totalAllowed then
                    Done updatedPicks
                else
                    Running(nextIdx, nextOrder, updatedPicks)
            { state with Phase = newPhase; Version = version }
        | _ -> state

    | DraftEventPayload.DraftEndedV1 _ ->
        match state.Phase with
        | Running(_, _, picks) -> { state with Phase = Done picks; Version = version }
        | _ -> state

let evolveFromEvents (events: DomainEvent<DraftEventPayload> list) : Draft =
    events |> List.fold evolve Unchecked.defaultof<Draft>
