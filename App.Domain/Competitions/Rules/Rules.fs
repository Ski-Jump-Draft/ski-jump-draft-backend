namespace App.Domain.Competitions.Rules

type Rules =
    | RawRules of Raw
    | Preset of Preset.Preset