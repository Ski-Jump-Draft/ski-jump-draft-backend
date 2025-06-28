module App.Domain.Game.Ranking

open App.Domain

type IRankingCreator =
    abstract member Create:
        gameId: Game.Game.Id * draftPicks: Draft.Picks.Picks * competitionResults: Competitions.Results ->
            Game.Game.EndedGameResults.Ranking
