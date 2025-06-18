namespace Game.Core.Domain.Games

open Game.Core.Domain
open Game.Core.Domain.Competitions
open Game.Core.Domain.Draft
open Game.Core.Domain.Shared
open Game.Core.Domain.Shared.Ids

module Game =
    type Error = | InvalidPhase

    [<Struct>]
    type Date = Date of System.DateTimeOffset

    type Rules = { MaxJumpersPerPlayer: uint }

    module EndedGameRanking =
        type Points = private Points of int

        module Points =
            let tryCreate (v: int) = if v >= 0 then Some(Points v) else None
            let value (v: Points) = v

        type Ranking = Ranking of Map<PlayerId, Points>

    type State =
        | NotStarted
        | Drafting of Draft.Definition
        | Simulating of Competition.Definition
        | Ended of Competition.Definition * EndedGameRanking.Ranking

    type Definition =
        { Id: GameId
          ServerId: ServerId
          Date: Date
          State: State
          Rules: Rules }

    let create serverId rules date =
        { Id = Id.newGameId ()
          ServerId = serverId
          Date = date
          State = State.NotStarted
          Rules = rules }

    let startDraft settings game : Result<Definition, Error> =
        match game.State with
        | NotStarted -> Draft.create settings |> fun d -> Ok { game with State = Drafting d }
        | _ -> Error InvalidPhase

    let startCompetition hillId rulesConfig game : Result<Definition, Error> =
        match game.State with
        | Drafting { Progress = Draft.Progress.Done _ } ->
            let competition = Competition.create hillId rulesConfig

            Ok
                { game with
                    State = State.Simulating competition }
        | _ -> Error InvalidPhase
