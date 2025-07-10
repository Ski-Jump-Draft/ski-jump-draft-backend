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
        | EndingMatchmakingTooFewPlayers of Current: uint * Min: uint
        | EndingMatchmakingTooManyPlayers of Current: uint * Max: uint
        | GameRoomFull
        | PlayerAlreadyJoined of Participant.Id

    type Participants = private Participants of Participant.Id list

    module Participants =
        let empty = Participants []

        let add player (Participants players) =
            if List.contains player players then
                Error(PlayerAlreadyJoined player)
            else
                Ok(Participants(player :: players))

        let remove player (Participants players) =
            Participants(List.filter ((<>) player) players)

        let contains player (Participants players) = List.contains player players

        let count (Participants players) : uint = uint (List.length players)
        let value (Participants players) = players


open Game

type Game =
    { Id: Id.Id
      Version: AggregateVersion
      HostId: Hosting.Host.Id
      Phase: Phase
      Settings: Settings.Settings
      Participants: Participants }

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

    member this.Join(playerId: Participant.Id) : Result<Game * GameEventPayload list, Error> =
        if this.ParticipantIsPresent(playerId) then
            Error(Game.Error.PlayerAlreadyJoined playerId)
        elif this.RoomIsFull then
            Error Game.Error.GameRoomFull
        else
            match this.Phase with
            | Matchmaking ->
                let updatedPlayers = Participants.add playerId this.Participants

                match updatedPlayers with
                | Ok updatedPlayers ->
                    let updatedGame =
                        { this with
                            Participants = updatedPlayers }

                    let event: ParticipantJoinedV1 =
                        { GameId = updatedGame.Id
                          PlayerId = playerId }

                    Ok(updatedGame, [ GameEventPayload.ParticipantJoinedV1 event ])
                | Error error -> Error(error)
            | _ -> Error(InvalidPhase([ MatchmakingTag ], this.Phase |> Game.TagOfPhase))

    // TODO: Wyjątki w CanJoin!!!!!!
    // TODO: jeśli gracz jest zbanowany, false
    // TODO: jeśli gra prywatna, podaj hasło
    // TODO: jeśli trzeba czekać na potwierdzenie, NIE WIEM CO

    member this.RoomIsFull =
        let currentPlayersCount = Participants.count this.Participants
        let playersLimit = Settings.PlayerLimit.value this.Settings.PlayerLimit
        currentPlayersCount >= playersLimit

    member this.ParticipantIsPresent(player: Participant.Id) =
        Participants.contains player this.Participants

    /// Matchmaking ID
    member this.StartMatchmaking =
        match this.Phase with
        | SettingUp ->
            let state = { this with Phase = Phase.Matchmaking }
            let event: MatchmakingPhaseStartedV1 = { GameId = this.Id }
            Ok(state, [ GameEventPayload.MatchmakingPhaseStartedV1 event ])
        | _ -> Error(InvalidPhase([ SettingUpTag ], Game.TagOfPhase this.Phase))

    member this.EndMatchmaking =
        match this.Phase with
        | Matchmaking ->
            let playersCount = Participants.count this.Participants
            let playersLimit = this.Settings.PlayerLimit
            let playersCountFitsLimit = Settings.PlayerLimit.fits playersCount playersLimit

            if playersCount >= 2u then
                if playersCountFitsLimit then
                    let state =
                        { this with
                            Phase = Break(Next = PhaseTag.PreDraftTag) }

                    let event: MatchmakingPhaseEndedV1 = { GameId = this.Id }

                    Ok(state, [ GameEventPayload.MatchmakingPhaseEndedV1 event ])
                else
                    Error(
                        EndingMatchmakingTooManyPlayers(
                            Participants.count this.Participants,
                            Settings.PlayerLimit.value playersLimit
                        )
                    )
            else
                Error(EndingMatchmakingTooFewPlayers(Participants.count this.Participants, 2u))
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

    member this.EndPreDraft =
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

    member this.EndDraft =
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

    member this.EndCompetition =
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
