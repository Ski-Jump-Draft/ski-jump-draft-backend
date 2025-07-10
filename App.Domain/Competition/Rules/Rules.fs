namespace App.Domain.Competition.Rules

type Rules =
    | RawRules of Raw
    | Preset of Preset.Preset