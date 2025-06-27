namespace App.Domain.Game

open System
open App.Domain
open App.Domain.Shared
open App.Domain.Shared.EventHelpers
open App.Domain.Time

// TODO: Ustawienia matchmakingu i pokoju gry. Game.Rules/Game.Settings
// TODO: Może dynamiczny globalny limit graczy na pokój?
// TODO: Pre-draft np. kwalifikacje

module Game =
    [<Struct>]
    type Id = Id of System.Guid

    module Participant =
        [<Struct>]
        type Id = Id of System.Guid

        type Name = private Name of string

        module Name =
            let tryCreate (v: string) =
                if v.Length >= 3 && v.Length <= 24 then
                    Ok(Name v)
                else
                    Error(invalidOp "Name must be 3-24 characters")

            let value (Name v) = v

    type Participant =
        { Id: Participant.Id
          Name: Participant.Name }

    [<Struct>]
    type Date = Date of System.DateTimeOffset

    module Date =
        type Error = CannotBeInFuture of DateTimeOffset

        let create (v: System.DateTimeOffset, clock: Time.IClock) =
            if v > clock.UtcNow then
                Error(CannotBeInFuture v)
            else
                Ok(Date v)

    module EndedGameResults =
        type Points = private Points of int

        module Points =
            let tryCreate (v: int) = if v >= 0 then Some(Points v) else None
            let value (v: Points) = v

        type Ranking = Ranking of Map<Participant.Id, Points>
        
    type Event =
        | PlayerJoined of GameId: Id * Timestamp: EventTimestamp * PlayerId: Participant.Id
        | PlayerLeft of GameId: Id * Timestamp: EventTimestamp * PlayerId: Participant.Id
        | MatchmakingPhaseStarted of GameId: Id * Timestamp: EventTimestamp // TODO: Matchmaking id
        | MatchmakingPhaseEnded of GameId: Id * Timestamp: EventTimestamp // TODO: Matchmaking id
        | PreDraftPhaseStarted of GameId: Id * Timestamp: EventTimestamp * PreDraftId: PreDraft.PreDraft.Id
        | PreDraftPhaseEnded of GameId: Id * Timestamp: EventTimestamp * PreDraftId: PreDraft.PreDraft.Id
        | DraftPhaseStarted of GameId: Id * Timestamp: EventTimestamp * DraftId: Draft.Draft.Id
        | DraftPhaseEnded of GameId: Id * Timestamp: EventTimestamp * DraftId: Draft.Draft.Id
        | CompetitionPhaseStarted of GameId: Id * Timestamp: EventTimestamp * CompetitionId: Game.Competition.Id
        | CompetitionPhaseEnded of GameId: Id * Timestamp: EventTimestamp * CompetitionId: Game.Competition.Id
        | GameEnded of GameId: Id * Timestamp: EventTimestamp * Results: EndedGameResults.Ranking

    type Phase =
        | SettingUp
        | Matchmaking
        | PreDraft of PreDraft.PreDraft.Id
        | Draft of Draft.Draft.Id
        | Competition of Game.Competition.Id
        | Ended of EndedGameResults.Ranking
        | Break of Next: PhaseTag
    // Jeśli gracz opuści, to go w 100% wymazujemy z gry

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

    module Settings =
        type Error =
            | PlayersLimitTooFew of uint
            | PlayersLimitTooMany of uint

        type PlayerLimit = private PlayerLimit of uint

        module PlayerLimit =
            let tryCreate (v: uint) =
                if v < 2u then Error(PlayersLimitTooFew(2u))
                elif v > 15u then Error(PlayersLimitTooMany(15u))
                else Ok(PlayerLimit v)

            let value (PlayerLimit v) = v

            let fits (count: uint) (limit: PlayerLimit) = uint count <= value limit

        module PhaseTransitionPolicy =
            type Error =
                | HostDecisionTimeoutTooLong of TimeSpan
                | AutostartAfterTimeTimeTooLong of TimeSpan
                | AutostartAfterTimeMinimalPlayersCountTooMany of uint

            type HostDecisionTimeout = private HostDecisionTimeout of TimeSpan

            module HostDecisionTimeout =
                let tryCreate (v: TimeSpan) =
                    if v <= TimeSpan.FromSeconds(120L) then
                        Ok(HostDecisionTimeout v)
                    else
                        Error(HostDecisionTimeoutTooLong v)

                let value (v: HostDecisionTimeout) = v

            module AutostartAfterTime =
                type Time = private Time of TimeSpan

                module Time =
                    let tryCreate (v: TimeSpan) =
                        if v <= TimeSpan.FromMinutes(5L) then
                            Ok v
                        else
                            Error(AutostartAfterTimeTimeTooLong v)

                    let value (v: Time) = v

                type MinimalPlayersCount = private MinimalPlayersCount of uint

                module MinimalPlayersCount =
                    let tryCreate (v: uint) =
                        if v < 1000u then
                            Ok v // TODO: GLOBAL_MAX_PLAYERS_IN_GAME / lub nie global, ale wciąż
                        else
                            Error(AutostartAfterTimeMinimalPlayersCountTooMany v)

                    let value (v: MinimalPlayersCount) = v

                type FailurePolicy =
                    | Retry
                    | DelegateControlToHost

            type StartingMatchmaking = | None // nie ma szczególnych metod na rozpoczęcie matchmakingu. TODO

            type EndingMatchmaking =
                | HostDecides of HostDecisionTimeout option
                | AutoAfter of
                    AutostartAfterTime.Time *
                    AutostartAfterTime.MinimalPlayersCount *
                    AutostartAfterTime.FailurePolicy
                | AutoWhenFull

            type StartingDraft =
                | HostDecides of HostDecisionTimeout
                | AutoAfter of TimeSpan

            type StartingSimulating =
                | HostDecides of HostDecisionTimeout
                | AutoAfter of TimeSpan

            type EndingSimulation = | None // nie ma szczególnych metod na zakończenie gry. TODO

    type Settings =
        { PlayerLimit: Settings.PlayerLimit
          DraftSettings: Draft.Draft.Settings // TODO: MaxJumpersPerPlayer: uint
          StartingMatchmakingPolicy: Settings.PhaseTransitionPolicy.StartingMatchmaking
          EndingMatchmakingPolicy: Settings.PhaseTransitionPolicy.EndingMatchmaking
          StartingDraftPolicy: Settings.PhaseTransitionPolicy.StartingDraft
          StartingSimulatingPolicy: Settings.PhaseTransitionPolicy.StartingSimulating
          EndingSimulationPolicy: Settings.PhaseTransitionPolicy.EndingSimulation }

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
    { Id: Id
      HostId: Hosting.Host.Id
      DateOfStart: Date
      Phase: Phase
      Settings: Settings
      Participants: Participants
      Clock: IClock }

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

    static member Create id (hostId, settings, clock: IClock) =
        { Id = id
          HostId = hostId
          DateOfStart = Date clock.UtcNow
          Phase = SettingUp
          Settings = settings
          Participants = Participants.empty
          Clock = clock }

    member this.Join(player: Participant.Id) : Result<Game * Event list, Error> =
        if this.ParticipantIsPresent(player) then
            Error(Game.Error.PlayerAlreadyJoined player)
        elif this.RoomIsFull then
            Error Game.Error.GameRoomFull
        else
            match this.Phase with
            | Matchmaking ->
                let updatedPlayers = Participants.add player this.Participants

                match updatedPlayers with
                | Ok updatedPlayers ->
                    let updatedGame =
                        { this with
                            Participants = updatedPlayers }

                    let event = Event.PlayerJoined(updatedGame.Id, this.Clock.UtcNow, player)
                    Ok(updatedGame, [ event ])
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

    member this.StartMatchmaking =
        match this.Phase with
        | SettingUp ->
            let state = { this with Phase = Phase.Matchmaking }
            let event = Event.MatchmakingPhaseStarted(this.Id, this.Clock.UtcNow)
            Ok(state, [ event ])
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

                    let event = Event.MatchmakingPhaseEnded(this.Id, this.Clock.UtcNow)
                    Ok(state, [ event ])
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

            let event = Event.PreDraftPhaseStarted(this.Id, this.Clock.UtcNow, preDraftId)
            Ok(state, [ event ])
        | _ -> Error(InvalidPhase([ PhaseTag.BreakTag PhaseTag.PreDraftTag ], Game.TagOfPhase(this.Phase)))

    member this.EndPreDraft =
        match this.Phase with
        | PreDraft preDraftId ->
            let state =
                { this with
                    Phase = Break PhaseTag.PreDraftTag }

            let event = Event.PreDraftPhaseEnded(this.Id, this.Clock.UtcNow, preDraftId)
            Ok(state, [ event ])
        | _ -> Error(InvalidPhase([ PhaseTag.PreDraftTag ], Game.TagOfPhase(this.Phase)))

    member this.StartDraft draftId =
        match this.Phase with
        | Break(Next = PhaseTag.DraftTag) ->
            let state =
                { this with
                    Phase = Phase.Draft draftId }

            let event = Event.DraftPhaseStarted(this.Id, this.Clock.UtcNow, draftId)
            Ok(state, [ event ])
        | _ -> Error(InvalidPhase([ BreakTag PhaseTag.DraftTag ], Game.TagOfPhase this.Phase))

    member this.EndDraft =
        match this.Phase with
        | Draft draftId ->
            let state =
                { this with
                    Phase = Phase.Break PhaseTag.CompetitionTag }

            let event = Event.DraftPhaseEnded(this.Id, this.Clock.UtcNow, draftId)
            Ok(state, [ event ])
        | _ -> Error(InvalidPhase([ DraftTag ], Game.TagOfPhase this.Phase))
        
    member this.StartCompetition competitionId =
        match this.Phase with
        | Break(Next = PhaseTag.CompetitionTag) ->
            let state =
                { this with
                    Phase = Phase.Competition competitionId }

            let event = Event.CompetitionPhaseStarted(this.Id, this.Clock.UtcNow, competitionId)
            Ok(state, [ event ])
        | _ -> Error(InvalidPhase([ BreakTag PhaseTag.CompetitionTag ], Game.TagOfPhase this.Phase))

    member this.EndCompetition =
        match this.Phase with
        | Competition competitionId ->
            let state =
                { this with
                    Phase = Phase.Break PhaseTag.EndedTag }

            let event = Event.CompetitionPhaseEnded(this.Id, this.Clock.UtcNow, competitionId)
            Ok(state, [ event ])
        | _ -> Error(InvalidPhase([ CompetitionTag ], Game.TagOfPhase this.Phase))
        
    member this.EndGame endedGameResults =
        match this.Phase with
        | Break(Next = PhaseTag.EndedTag) ->
            let state = { this with Phase = Phase.Ended endedGameResults }
            let event = Event.GameEnded(this.Id, this.Clock.UtcNow, endedGameResults)
            Ok(state, [event])
        | _ -> Error(InvalidPhase([ BreakTag PhaseTag.EndedTag ], Game.TagOfPhase this.Phase))