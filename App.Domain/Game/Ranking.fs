module App.Domain.Game.Ranking

open App.Domain

type IRankingCreator =
    abstract member Create:
        gameId: Game.Game.Id * draftPicks: Draft.Draft.Picks * competitionResults: Competition.Competition.Results ->
            Game.Game.EndedGameResults.Ranking
