module App.Domain.Matchmaking.Evolve

open App.Domain
open App.Domain.Matchmaking
open App.Domain.Matchmaking.Event
open App.Domain.Matchmaking
open App.Domain.Shared
open App.Domain.Shared.AggregateVersion

let evolve (state: Matchmaking) =
    function
    | MatchmakingEventPayload.MatchmakingCreatedV1 e ->
        { Id = e.MatchmakingId
          Version = AggregateVersion 0u
          Settings = e.Settings
          Phase = Phase.Active Set.empty }

    | MatchmakingEventPayload.MatchmakingPlayerJoinedV1 e ->
        match state.Phase with
        | Active players ->
            { state with Phase = Active(players |> Set.add e.ParticipantId) }
        | _ -> state // nie powinno się zdarzyć

    | MatchmakingEventPayload.MatchmakingPlayerLeftV1 e ->
        match state.Phase with
        | Active players ->
            { state with Phase = Active(players |> Set.remove e.ParticipantId) }
        | _ -> state

    | MatchmakingEventPayload.MatchmakingEndedV1 _ ->
        match state.Phase with
        | Active players ->
            { state with Phase = Ended players }
        | _ -> state

    | MatchmakingEventPayload.MatchmakingFailedV1 e ->
        match state.Phase with
        | Active players ->
            { state with Phase = Failed(players, e.Error) }
        | _ -> state

let evolveFromEvents (events: DomainEvent<MatchmakingEventPayload> list) : Matchmaking =
    events
    |> List.map (fun e -> e.Payload)
    |> List.fold evolve Unchecked.defaultof<Matchmaking> // MatchmakingCreatedV1 musi być pierwsze
