namespace App.Domain.Competition.Rules

type Config =
    | RawRules of Raw
    | Preset of Preset.Preset