namespace App.Domain.Game

open System
open App.Domain
open App.Domain.Game.Event
open App.Domain.Game.Ranking
open App.Domain.Shared.AggregateVersion

// TODO: Ustawienia matchmakingu i pokoju gry. Game.Rules/Game.Settings
// TODO: Może dynamiczny globalny limit graczy na pokój?
// TODO: Pre-draft np. kwalifikacje

module Game =
    [<Struct>]
    type Date = Date of System.DateTimeOffset

    module Date =
        type Error = CannotBeInFuture of DateTimeOffset

        let create (v: System.DateTimeOffset, clock: Time.IClock) =
            if v > clock.UtcNow then
                Error(CannotBeInFuture v)
            else
                Ok(Date v)

    type Phase =
        | SettingUp
        | Matchmaking
        | PreDraft of PreDraft.Id.Id
        | Draft of Draft.Id.Id
        | Competition of Game.Competition.Id
        | Ended of EndedGameResults.Ranking
        | Break of Next: PhaseTag

    and PhaseTag =
        | SettingUpTag
        | MatchmakingTag
        | PreDraftTag
        | DraftTag
        | CompetitionTag
        | EndedTag
        | BreakTag of Next: PhaseTag

    type Error =
        | InvalidPhase of Expected: PhaseTag list * Actual: PhaseTag
        | HostDecisionTimeout of TimeSpan
        | EndingMatchmakingTooFewParticipants of Current: uint * Min: uint
        | EndingMatchmakingTooManyParticipants of Current: uint * Max: uint
        | GameRoomFull
        | ParticipantAlreadyJoined of Participant.Id
        | ParticipantNotInGame of Participant.Id

    type Participants = private Participants of Participant.Id list

    module Participants =
        let empty = Participants []

        let add participant (Participants participants) =
            if List.contains participant participants then
                Error(ParticipantAlreadyJoined participant)
            else
                Ok(Participants(participant :: participants))

        let remove participant (Participants participants) =
            Participants(List.filter ((<>) participant) participants)

        let contains participant (Participants participants) = List.contains participant participants

        let count (Participants participants) : uint = uint (List.length participants)
        let value (Participants participants) = participants


open Game

type Game =
    private
        { Id: Id.Id
          Version: AggregateVersion
          HostId: Hosting.Host.Id
          Phase: Phase
          Settings: Settings.Settings
          Participants: Participants }

    member this.Phase_ = this.Phase
    member this.Participants_ = this.Participants
    member this.Settings_ = this.Settings
    member this.Version_: AggregateVersion = this.Version
    member this.Id_ = this.Id

    static member TagOfPhase phase =
        match phase with
        | SettingUp -> SettingUpTag
        | Matchmaking -> MatchmakingTag
        | PreDraft _ -> PreDraftTag
        | Draft _ -> DraftTag
        | Competition _ -> CompetitionTag
        | Ended _ -> EndedTag
        | Break next -> BreakTag next
    //| Break _ -> failwith "No Phase.Break equivalent in PhaseTag"

    static member Create id version (hostId, settings) : Result<Game * GameEventPayload list, Error> =
        let state =
            { Id = id
              Version = version
              HostId = hostId
              Phase = SettingUp
              Settings = settings
              Participants = Participants.empty }

        let event: Event.GameCreatedV1 =
            { GameId = id
              HostId = hostId
              Settings = settings }


        Ok(state, [ GameEventPayload.GameCreatedV1 event ])

    member this.Join(participantId: Participant.Id) : Result<Game * GameEventPayload list, Error> =
        if this.ParticipantIsPresent(participantId) then
            Error(Game.Error.ParticipantAlreadyJoined participantId)
        elif this.RoomIsFull then
            Error Game.Error.GameRoomFull
        else
            match this.Phase with
            | Matchmaking ->
                let updatedParticipants = Participants.add participantId this.Participants

                match updatedParticipants with
                | Ok updatedParticipants ->
                    let updatedGame =
                        { this with
                            Participants = updatedParticipants }

                    let event: ParticipantJoinedV1 =
                        { GameId = updatedGame.Id
                          ParticipantId = participantId }

                    Ok(updatedGame, [ GameEventPayload.ParticipantJoinedV1 event ])
                | Error error -> Error(error)
            | _ -> Error(InvalidPhase([ MatchmakingTag ], this.Phase |> Game.TagOfPhase))

    member this.Leave(participantId: Participant.Id) : Result<Game * GameEventPayload list, Error> =
        if not (this.ParticipantIsPresent(participantId)) then
            Error(Game.Error.ParticipantNotInGame participantId)
        else
            match this.Phase with
            | Matchmaking
            | Break _
            | Draft _
            | Competition _
            | PreDraft _ ->
                let updatedParticipants = Participants.remove participantId this.Participants

                let updatedGame =
                    { this with
                        Participants = updatedParticipants }

                let event: ParticipantLeftV1 =
                    { GameId = updatedGame.Id
                      ParticipantId = participantId }

                Ok(updatedGame, [ GameEventPayload.ParticipantLeftV1 event ])
            | _ ->
                Error(
                    InvalidPhase(
                        [ PhaseTag.MatchmakingTag
                          (PhaseTag.BreakTag PhaseTag.CompetitionTag)
                          (PhaseTag.BreakTag PhaseTag.DraftTag)
                          (PhaseTag.BreakTag PhaseTag.PreDraftTag)
                          PhaseTag.DraftTag
                          PhaseTag.CompetitionTag
                          PhaseTag.PreDraftTag ],
                        this.Phase |> Game.TagOfPhase
                    )
                )


    // TODO: Wyjątki w CanJoin!!!!!!
    // TODO: jeśli gracz jest zbanowany, false
    // TODO: jeśli gra prywatna, podaj hasło
    // TODO: jeśli trzeba czekać na potwierdzenie, NIE WIEM CO

    member this.RoomIsFull =
        let currentParticipantsCount = Participants.count this.Participants

        let participantsLimit =
            Settings.ParticipantLimit.value this.Settings.ParticipantLimit

        currentParticipantsCount >= participantsLimit

    member this.ParticipantIsPresent(participant: Participant.Id) =
        Participants.contains participant this.Participants

    /// Matchmaking ID
    member this.StartMatchmaking() =
        match this.Phase with
        | SettingUp ->
            let state = { this with Phase = Phase.Matchmaking }
            let event: MatchmakingPhaseStartedV1 = { GameId = this.Id }
            Ok(state, [ GameEventPayload.MatchmakingPhaseStartedV1 event ])
        | _ -> Error(InvalidPhase([ SettingUpTag ], Game.TagOfPhase this.Phase))

    member this.EndMatchmaking() =
        match this.Phase with
        | Matchmaking ->
            let participantsCount = Participants.count this.Participants
            let participantsLimit = this.Settings.ParticipantLimit

            let participantsCountFitsLimit =
                Settings.ParticipantLimit.fits participantsCount participantsLimit

            if participantsCount >= 2u then
                if participantsCountFitsLimit then
                    let state =
                        { this with
                            Phase = Break(Next = PhaseTag.PreDraftTag) }

                    let event: MatchmakingPhaseEndedV1 = { GameId = this.Id }

                    Ok(state, [ GameEventPayload.MatchmakingPhaseEndedV1 event ])
                else
                    Error(
                        EndingMatchmakingTooManyParticipants(
                            Participants.count this.Participants,
                            Settings.ParticipantLimit.value participantsLimit
                        )
                    )
            else
                Error(EndingMatchmakingTooFewParticipants(Participants.count this.Participants, 2u))
        | _ -> Error(InvalidPhase([ MatchmakingTag ], Game.TagOfPhase this.Phase))

    member this.StartPreDraft(preDraftId) =
        match this.Phase with
        | Break PhaseTag.PreDraftTag ->
            let state =
                { this with
                    Phase = PreDraft preDraftId } // TODO

            let event: PreDraftPhaseStartedV1 =
                { GameId = this.Id
                  PreDraftId = preDraftId }

            Ok(state, [ GameEventPayload.PreDraftPhaseStartedV1 event ])
        | _ -> Error(InvalidPhase([ PhaseTag.BreakTag PhaseTag.PreDraftTag ], Game.TagOfPhase(this.Phase)))

    member this.EndPreDraft() =
        match this.Phase with
        | PreDraft preDraftId ->
            let state =
                { this with
                    Phase = Break PhaseTag.PreDraftTag }

            let event: PreDraftPhaseEndedV1 =
                { GameId = this.Id
                  PreDraftId = preDraftId }

            Ok(state, [ GameEventPayload.PreDraftPhaseEndedV1 event ])
        | _ -> Error(InvalidPhase([ PhaseTag.PreDraftTag ], Game.TagOfPhase(this.Phase)))

    member this.StartDraft draftId =
        match this.Phase with
        | Break(Next = PhaseTag.DraftTag) ->
            let state =
                { this with
                    Phase = Phase.Draft draftId }

            let event: DraftPhaseStartedV1 = { GameId = this.Id; DraftId = draftId }
            Ok(state, [ GameEventPayload.DraftPhaseStartedV1 event ])
        | _ -> Error(InvalidPhase([ BreakTag PhaseTag.DraftTag ], Game.TagOfPhase this.Phase))

    member this.EndDraft() =
        match this.Phase with
        | Draft draftId ->
            let state =
                { this with
                    Phase = Phase.Break PhaseTag.CompetitionTag }

            let event: DraftPhaseEndedV1 = { GameId = this.Id; DraftId = draftId }
            Ok(state, [ GameEventPayload.DraftPhaseEndedV1 event ])
        | _ -> Error(InvalidPhase([ DraftTag ], Game.TagOfPhase this.Phase))

    member this.StartCompetition competitionId =
        match this.Phase with
        | Break(Next = PhaseTag.CompetitionTag) ->
            let state =
                { this with
                    Phase = Phase.Competition competitionId }

            let event: CompetitionPhaseStartedV1 =
                { GameId = this.Id
                  CompetitionId = competitionId }

            Ok(state, [ GameEventPayload.CompetitionPhaseStartedV1 event ])

        | _ -> Error(InvalidPhase([ BreakTag PhaseTag.CompetitionTag ], Game.TagOfPhase this.Phase))

    member this.EndCompetition() =
        match this.Phase with
        | Competition competitionId ->
            let state =
                { this with
                    Phase = Phase.Break PhaseTag.EndedTag }

            let event: CompetitionPhaseEndedV1 =
                { GameId = this.Id
                  CompetitionId = competitionId }

            Ok(state, [ GameEventPayload.CompetitionPhaseEndedV1 event ])
        | _ -> Error(InvalidPhase([ CompetitionTag ], Game.TagOfPhase this.Phase))

    member this.EndGame endedGameResults =
        match this.Phase with
        | Break(Next = PhaseTag.EndedTag) ->
            let state =
                { this with
                    Phase = Phase.Ended endedGameResults }

            let event: GameEndedV1 =
                { GameId = this.Id
                  Results = endedGameResults }

            Ok(state, [ GameEventPayload.GameEndedV1 event ])
        | _ -> Error(InvalidPhase([ BreakTag PhaseTag.EndedTag ], Game.TagOfPhase this.Phase))
