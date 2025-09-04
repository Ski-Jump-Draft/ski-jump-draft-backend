namespace App.Domain.Game

open App.Domain

type RankingPolicy =
    | Classic
    | PodiumAtAllCosts

type PreDraftSettings = private {
    Competitions: Competition.Settings list
} with
    static member Create (competitionSettingsList: Competition.Settings list) =
        if competitionSettingsList.Length > 0 && competitionSettingsList.Length <= 2 then    
            Some({
                Competitions = competitionSettingsList
            })
        else
            None
            
type Settings = {
    PreDraftSettings: PreDraftSettings
    DraftSettings: Draft.Settings
    MainCompetitionSettings: Competition.Settings
    RankingPolicy: RankingPolicy
}

