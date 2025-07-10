namespace App.Domain.PreDraft.Settings

open App.Domain.PreDraft.Competitions

type Settings =
    { Competitions: Competition.Settings list }
