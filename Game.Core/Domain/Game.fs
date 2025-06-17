namespace Game.Core.Domain

open System
open Ids

type GameRules = { MaxJumpersPerPlayer: uint }

type GameState =
    | NotStarted
    | Drafting
    | SimulatingCompetition
    | Completed of ranking: Map<PlayerId, int>

type Game =
    { Id: GameId
      ServerId: ServerId
      Date: DateTimeOffset
      State: GameState
      Rules: GameRules
      Draft: Draft option
      Competition: Competition option }

module Game =
    let create serverId rules date=
        { Id = Id.newGameId()
          ServerId = serverId
          Date = date
          State = GameState.NotStarted
          Rules = rules
          Draft = None
          Competition = None }
        
    let startDraft draftSettings game =
        let draft = Draft.create draftSettings
        { game with Draft = Some draft; State = GameState.Drafting }
        
    let startCompetition competitionSettings game =
        { game with Competition = Competition.create competitionSettings; State = GameState.SimulatingCompetition }
        

// ----------TODO----------
// • Competition rules modules
// • Domain events
// • Validation & error types
// • Tests (Expecto / xUnit)
// • Application services and ports
