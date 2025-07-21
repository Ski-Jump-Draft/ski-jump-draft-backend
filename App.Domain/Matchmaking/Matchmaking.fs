namespace App.Domain.Matchmaking

open App.Domain.Matchmaking.Event
open App.Domain.Shared.AggregateVersion

module internal Internal =

    let tag =
        function
        | Active _ -> ActiveTag
        | Failed _ -> FailedTag
        | Ended _ -> EndedTag

    let expect phases actual = Error(InvalidPhase(phases, tag actual))

    let ok state (events: Event.MatchmakingEventPayload list) = Ok(state, events)

open Internal

type Matchmaking =
    private
        { Id: Id
          Version: AggregateVersion
          Settings: Settings
          Phase: Phase }

    member this.Phase_ = this.Phase
    member this.Settings_ = this.Settings
    member this.Version_: AggregateVersion = this.Version
    member this.Id_ = this.Id

    static member Create id version settings : Result<Matchmaking * MatchmakingEventPayload list, Error> =
        let state =
            { Id = id
              Version = version
              Settings = settings
              Phase = Phase.Active Set.empty }

        let event: MatchmakingCreatedV1 =
            { MatchmakingId = id
              Settings = settings }

        Ok(state, [ MatchmakingEventPayload.MatchmakingCreatedV1 event ])

    member this.Join playerId =
        match this.Phase with
        | Active players ->
            if not this.CanJoin then
                match this.RoomIsFull with
                | Ok roomIsFull ->
                    if roomIsFull then
                        let playersCount = players |> Set.count
                        Error(Error.RoomFull playersCount)
                    else
                        Error(Error.InternalError)
                | Error error -> Error(error)
            elif players |> Set.contains playerId then
                Error(Error.PlayerAlreadyJoined playerId)
            else
                let state =
                    { this with
                        Phase = Active(players |> Set.add playerId) }

                let events =
                    [ Event.MatchmakingPlayerJoinedV1
                          { MatchmakingId = this.Id
                            ParticipantId = playerId } ]

                ok state events

        | phase -> expect [ ActiveTag ] phase

    member this.RoomIsFull: Result<bool, Error> =
        match this.Phase with
        | Active players ->
            let limit = PlayersCount.value this.Settings.MaxPlayersCount
            Ok((players |> Set.count) = limit)
        | phase -> expect [ ActiveTag ] phase

    member this.CanJoin =
        match this.Phase with
        | Active players ->
            match this.RoomIsFull with
            | Ok roomIsFull -> not roomIsFull
            | Error error -> false
        | _ -> false

    member this.Leave playerId =
        match this.Phase with
        | Active players ->
            if not (players |> Set.contains playerId) then
                Error(Error.PlayerNotJoined playerId)
            else
                let state =
                    { this with
                        Phase = Active(players |> Set.remove playerId) }

                let events =
                    [ Event.MatchmakingPlayerLeftV1
                          { MatchmakingId = this.Id
                            ParticipantId = playerId } ]

                ok state events

        | phase -> expect [ ActiveTag ] phase

    member this.End =
        match this.Phase with
        | Active players ->
            let playersCount = players |> Set.count
            let state = { this with Phase = Ended players }

            let events =
                [ Event.MatchmakingEndedV1
                      { MatchmakingId = this.Id
                        PlayersCount = playersCount } ]

            ok state events

        | phase -> expect [ ActiveTag ] phase

    member this.EndWithFailure reason =
        match this.Phase with
        | Active players ->
            let playersCount = players |> Set.count

            let state =
                { this with
                    Phase = Failed(players, reason) }

            let events =
                [ Event.MatchmakingFailedV1
                      { MatchmakingId = this.Id
                        PlayersCount = playersCount
                        Error = reason } ]

            ok state events

        | phase -> expect [ ActiveTag ] phase
