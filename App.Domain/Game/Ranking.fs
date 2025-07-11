module App.Domain.Game.Ranking

open App.Domain

module EndedGameResults =
    type Points = private Points of int

    module Points =
        let tryCreate (v: int) = if v >= 0 then Some(Points v) else None
        let value (v: Points) = v

    type Ranking = Ranking of Map<Participant.Id, Points>

type IRankingCreator =
    abstract member Create: gameId: Game.Id.Id -> EndedGameResults.Ranking
