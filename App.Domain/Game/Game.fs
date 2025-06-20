namespace App.Domain.Game

open System
open App.Domain
open App.Domain.Player
open App.Domain.Shared
open App.Domain.Shared.Ids
open App.Domain.Shared.Random
open App.Domain.Time

// TODO: Ustawienia matchmakingu i pokoju gry. Game.Rules/Game.Settings
// TODO: Może dynamiczny globalny limit graczy na pokój?
// TODO: Pre-draft np. kwalifikacje

module Game =
    // pomocnicze podtypy
    [<Struct>]
    type Date = Date of System.DateTimeOffset

    module Date =
        type Error =
            | CannotBeInFuture of DateTimeOffset
        let create (v: System.DateTimeOffset, clock: Time.IClock) =
            if v > clock.UtcNow then
                Error (CannotBeInFuture v)
            else
                Ok(Date v)

    module EndedGameRanking =
        type Points = private Points of int

        module Points =
            let tryCreate (v: int) = if v >= 0 then Some(Points v) else None
            let value (v: Points) = v

        type Ranking = Ranking of Map<PlayerId, Points>

    type Phase =
        | SettingUp
        | Matchmaking
        | NotStarted
        | Drafting of Draft.Draft
        | Simulating of Competition.Competition
        | Ended of Competition.Competition * EndedGameRanking.Ranking
    // Jeśli gracz opuści, to go w 100% wymazujemy z gry

    and PhaseTag =
        | SettingUpTag
        | MatchmakingTag
        | NotStartedTag
        | DraftingTag
        | SimulatingTag
        | EndedTag

    type Error =
        | InvalidPhase of Expected: PhaseTag list * Actual: PhaseTag
        | HostDecisionTimeout of TimeSpan
        | EndingMatchmakingTooFewPlayers of uint

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

    type Players = private Players of PlayerId list

    module Players =
        type Error = AlreadyJoined of PlayerId
        let empty = Players []

        let add player (Players players) =
            if List.contains player players then
                Error(AlreadyJoined player)
            else
                Ok(Players(player :: players))

        let remove player (Players players) =
            Players(List.filter ((<>) player) players)

        let count (Players players) : uint = uint (List.length players)
        let value (Players players) = players


open Game

type Game =
    { Id: GameId
      HostId: HostId
      Date: Date
      Phase: Phase
      Settings: Settings
      Players: Players
      IdGen: IGuid
      }

    static member TagOfPhase phase =
        match phase with
        | SettingUp -> SettingUpTag
        | Matchmaking -> MatchmakingTag
        | NotStarted -> NotStartedTag
        | Drafting _ -> DraftingTag
        | Simulating _ -> SimulatingTag
        | Ended _ -> EndedTag

    static member Create(idGen: IGuid) (hostId, rules, clock: IClock) =
        { Id = Id.newGameId idGen
          HostId = hostId
          Date = Date clock.UtcNow
          Phase = SettingUp
          Settings = rules
          Players = Players.empty
          IdGen = idGen
          }

    member this.CanJoin(player: Player) =
        if this.RoomIsFull then
            false
        else
            match this.Phase with
            | Matchmaking -> true
            | NotStarted -> false // TODO: może last minute?
            | _ -> false

    // TODO: Wyjątki w CanJoin!!!!!!
    // TODO: jeśli gracz jest zbanowany, false
    // TODO: jeśli gra prywatna, podaj hasło
    // TODO: jeśli trzeba czekać na potwierdzenie, NIE WIEM CO

    member this.RoomIsFull =
        let currentPlayersCount = Players.count this.Players
        let playersLimit = Settings.PlayerLimit.value this.Settings.PlayerLimit
        currentPlayersCount >= playersLimit // condition, nie przypisanie

    member this.EndMatchmaking =
        match this.Phase with
        | Matchmaking ->
            let playersCount = Players.count this.Players
            let playersCountFitsLimit = Settings.PlayerLimit.fits playersCount this.Settings.PlayerLimit
            if playersCount >= 2u && playersCountFitsLimit then
                Ok { this with Phase = NotStarted }
            else
                Error(EndingMatchmakingTooFewPlayers(Players.count this.Players))
        | _ -> Error(InvalidPhase([ MatchmakingTag ], Game.TagOfPhase this.Phase))

    // TODO: member this.StartObservationSession

    member this.StartDraft(settings, random: IRandom) : Result<Game, Error> =
        match this.Phase with
        | NotStarted -> (Draft.Draft.Create this.IdGen settings random) |> fun d -> Ok { this with Phase = Drafting d }
        | _ -> Error(InvalidPhase([ NotStartedTag ], Game.TagOfPhase this.Phase))

    member this.StartSimulation(hillId, rulesConfig) : Result<Game, Error> =
        match this.Phase with
        | Drafting { Progress = Draft.Draft.Done _ } ->
            let comp = Competition.Competition.Create this.IdGen hillId rulesConfig
            Ok { this with Phase = Simulating comp }
        | _ -> Error(InvalidPhase([ DraftingTag ], Game.TagOfPhase this.Phase))
