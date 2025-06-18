namespace Game.Core.Domain.Competitions

type RulesConfig =
    | RawRules of RawRules.Definition
    | Preset of RulesPreset.Definition