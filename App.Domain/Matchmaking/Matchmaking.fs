namespace App.Domain.Matchmaking

open App.Domain.Matchmaking.Event
open App.Domain.Shared.AggregateVersion

module internal Internal =
    let tag =
        function
        | Active -> ActiveTag
        | Failed _ -> FailedTag
        | Ended -> EndedTag

    let expect phases actual = Error(InvalidPhase(phases, tag actual))
    let ok state (events: Event.MatchmakingEventPayload list) = Ok(state, events)

open Internal

type Matchmaking =
    private
        { Id: Id
          Version: AggregateVersion
          Settings: Settings
          Phase: Phase
          Participants: Set<Participant> }

    member this.Phase_ = this.Phase
    member this.Settings_ = this.Settings
    member this.Version_: AggregateVersion = this.Version
    member this.Id_ = this.Id

    static member Create id version settings : Result<Matchmaking * MatchmakingEventPayload list, Error> =
        let state =
            { Id = id
              Version = version
              Settings = settings
              Phase = Phase.Active
              Participants = Set.empty }

        let event: MatchmakingCreatedV1 =
            { MatchmakingId = id
              Settings = settings }

        Ok(state, [ MatchmakingEventPayload.MatchmakingCreatedV1 event ])

    member this.Join(participant: Participant) =
        match this.Phase with
        | Active ->
            if not this.CanJoin then
                Error(Error.RoomFull(this.ParticipantsCount))
            elif this.Participants |> Set.contains participant then
                Error(Error.ParticipantAlreadyJoined participant.Id)
            else
                let nextVersion = increment this.Version

                let newParticipants = this.Participants |> Set.add participant

                let state =
                    { this with
                        Participants = newParticipants
                        Version = nextVersion }

                let events =
                    [ Event.MatchmakingParticipantJoinedV1
                          { MatchmakingId = this.Id
                            Participant =
                              { Id = participant.Id
                                Nick = participant.Nick } } ]

                ok state events
        | phase -> expect [ ActiveTag ] phase

    member this.Leave(participantId: Participant.Id) =
        let participantIsPresent = this.ParticipantIsPresent participantId

        match this.Phase with
        | Active when participantIsPresent ->
            let nextVersion = increment this.Version

            let nextParticipants =
                this.Participants |> Set.filter (fun p -> p.Id <> participantId)

            let state =
                { this with
                    Participants = nextParticipants
                    Version = nextVersion }

            let events =
                [ Event.MatchmakingParticipantLeftV1
                      { MatchmakingId = this.Id
                        ParticipantId = participantId } ]

            ok state events
        | Active when not (participantIsPresent) -> Error(Error.ParticipantNotInMatchmaking participantId)
        | phase -> expect [ ActiveTag ] phase

    member this.TryEnd(currentDuration: Duration) =
        let participantsCount = this.ParticipantsCount

        match this.Phase with
        | Active ->
            let nextVersion = increment this.Version

            if this.ShouldEnd currentDuration then
                let state =
                    { this with
                        Phase = Ended
                        Version = nextVersion }

                let events =
                    [ Event.MatchmakingEndedV1
                          { MatchmakingId = this.Id
                            ParticipantsCount = participantsCount } ]

                ok state events
            else
                let failReason =
                    MatchmakingFailReason.NotEnoughPlayers(participantsCount, this.MinimumParticipantsCount)

                let state =
                    { this with
                        Phase = Failed(failReason)
                        Version = nextVersion }

                let events =
                    [ Event.MatchmakingFailedV1
                          { MatchmakingId = this.Id
                            ParticipantsCount = participantsCount
                            Reason = failReason } ]

                ok state events
        | phase -> expect [ ActiveTag ] phase

    member this.EndWithInternalError details =
        match this.Phase with
        | Active ->
            let nextVersion = increment this.Version

            let state =
                { this with
                    Phase = Failed(MatchmakingFailReason.InternalError details)
                    Version = nextVersion }

            let events =
                [ Event.MatchmakingFailedV1
                      { MatchmakingId = this.Id
                        ParticipantsCount = this.ParticipantsCount
                        Reason = MatchmakingFailReason.InternalError details } ]

            ok state events
        | phase -> expect [ ActiveTag ] phase

    member this.RoomIsFull: bool =
        let limit = PlayersCount.value this.Settings.MinParticipants
        this.ParticipantsCount = limit

    member this.ShouldEnd(currentDuration: Duration) : bool =
        let maxParticipants = PlayersCount.value this.Settings.MinParticipants

        this.Phase.IsActive
        && (this.ParticipantsCount >= maxParticipants
            || currentDuration > this.Settings.MaxDuration)

    member this.CanJoin: bool =
        match this.Phase with
        | Active -> not this.RoomIsFull
        | _ -> false

    member this.ParticipantsCount: int = this.Participants |> Set.count

    member this.MinimumParticipantsCount: int =
        PlayersCount.value this.Settings.MaxParticipants

    member this.ReachedMinimum: bool =
        this.ParticipantsCount >= this.MinimumParticipantsCount

    member this.ParticipantIsPresent(participantId: Participant.Id) : bool =
        this.Participants |> Set.exists (fun p -> p.Id = participantId)

    member this.ParticipantIsPresent(participant: Participant) : bool =
        this.Participants |> Set.contains participant
