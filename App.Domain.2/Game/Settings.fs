namespace App.Domain._2.Game

open App.Domain._2

type RankingPolicy =
    | Classic
    | PodiumAtAllCosts

type Settings = {
    PreDraftSettings: Competition.Settings
    DraftSettings: Draft.Settings
    MainCompetitionSettings: Competition.Settings
    RankingPolicy: RankingPolicy
}

