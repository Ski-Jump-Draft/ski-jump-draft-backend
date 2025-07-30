module App.Domain.Matchmaking.Evolve

open App.Domain
open App.Domain.Matchmaking
open App.Domain.Matchmaking.Event
open App.Domain.Matchmaking
open App.Domain.Shared
open App.Domain.Shared.AggregateVersion

let evolve (state: Matchmaking) (event: DomainEvent<MatchmakingEventPayload>) =
    let version = AggregateVersion event.Header.AggregateVersion

    match event.Payload with
    | MatchmakingEventPayload.MatchmakingCreatedV1 e ->
        { Id = e.MatchmakingId
          Version = version
          Settings = e.Settings
          Phase = Phase.Active Set.empty }

    | MatchmakingEventPayload.MatchmakingPlayerJoinedV1 e ->
        match state.Phase with
        | Active players ->
            { state with
                Phase = Active(Set.add e.ParticipantId players)
                Version = version }
        | _ -> state

    | MatchmakingEventPayload.MatchmakingPlayerLeftV1 e ->
        match state.Phase with
        | Active players ->
            { state with
                Phase = Active(Set.remove e.ParticipantId players)
                Version = version }
        | _ -> state

    | MatchmakingEventPayload.MatchmakingEndedV1 _ ->
        match state.Phase with
        | Active players ->
            { state with
                Phase = Ended players
                Version = version }
        | _ -> state

    | MatchmakingEventPayload.MatchmakingFailedV1 e ->
        match state.Phase with
        | Active players ->
            { state with
                Phase = Failed(players, e.Error)
                Version = version }
        | _ -> state

let evolveFromEvents (events: DomainEvent<MatchmakingEventPayload> list) : Matchmaking =
    events |> List.fold evolve Unchecked.defaultof<Matchmaking>
