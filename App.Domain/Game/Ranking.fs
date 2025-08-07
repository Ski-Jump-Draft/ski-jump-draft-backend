module App.Domain.Game.Ranking

open System.Threading.Tasks
open App.Domain

module GameRanking =
    type Points = private Points of int

    module Points =
        let tryCreate (v: int) = if v >= 0 then Some(Points v) else None
        let value (v: Points) = v


type GameRanking = Ranking of Map<Participant.Id, GameRanking.Points>

type IGameRankingFactory =
    abstract member Create: gameId: Game.Id.Id -> Task<GameRanking>