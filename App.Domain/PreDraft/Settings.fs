namespace App.Domain.PreDraft.Settings

open App.Domain.PreDraft.Competitions

type Settings =
    { CompetitionSettings: Competition.Settings list }
