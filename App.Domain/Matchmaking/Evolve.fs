module App.Domain.Matchmaking.Evolve

open App.Domain.Shared
open App.Domain.Shared.AggregateVersion
open App.Domain.Matchmaking
open App.Domain.Matchmaking.Event

let evolve (state: Matchmaking) (event: DomainEvent<MatchmakingEventPayload>) : Matchmaking =
    let version = AggregateVersion event.Header.AggregateVersion

    match event.Payload with
    | MatchmakingCreatedV1 e ->
        { Id = e.MatchmakingId
          Version = version
          Settings = e.Settings
          Phase = Active
          Participants = Set.empty }

    | MatchmakingParticipantJoinedV1 e ->
        let participant =
            { Id = e.Participant.Id
              Nick = e.Participant.Nick }

        { state with
            Participants =
                state.Participants
                |> Set.add
                    { Id = participant.Id
                      Nick = participant.Nick }
            Version = version }

    | MatchmakingParticipantLeftV1 e ->
        { state with
            Participants = state.Participants |> Set.filter (fun p -> p.Id <> e.ParticipantId)
            Version = version }

    | MatchmakingEndedV1 _ ->
        { state with
            Phase = Ended
            Version = version }

    | MatchmakingFailedV1 e ->
        { state with
            Phase = Failed e.Reason
            Version = version }

let evolveFromEvents (events: DomainEvent<MatchmakingEventPayload> list) : Matchmaking =
    events |> List.fold evolve Unchecked.defaultof<Matchmaking>
