namespace App.Domain.Game

open System
open App.Domain

type RankingPolicy =
    | Classic
    | PodiumAtAllCosts

type PreDraftSettings =
    private
        { Competitions: Competition.Settings list }

    static member Create(competitionSettingsList: Competition.Settings list) =
        if competitionSettingsList.Length > 0 && competitionSettingsList.Length <= 2 then
            Some({ Competitions = competitionSettingsList })
        else
            None

    member this.CompetitionsCount = this.Competitions.Length
    member this.Competitions_: Competition.Settings list = this.Competitions


type PhaseDuration =
    private
        { PhaseDuration: TimeSpan }

    static member Create(v: TimeSpan) =
        if v.TotalSeconds > 0 then
            Some({ PhaseDuration = v })
        else
            None

    member this.Value = this.PhaseDuration

type BreakSettings =
    { BreakBeforePreDraft: PhaseDuration
      BreakBetweenPreDraftCompetitions: PhaseDuration
      BreakBeforeDraft: PhaseDuration
      BreakBeforeMainCompetition: PhaseDuration
      BreakBeforeEnd: PhaseDuration }

type Settings =
    { BreakSettings: BreakSettings
      PreDraftSettings: PreDraftSettings
      DraftSettings: Draft.Settings
      MainCompetitionSettings: Competition.Settings
      CompetitionJumpInterval: PhaseDuration
      RankingPolicy: RankingPolicy }
