namespace Game.Core.Domain.Competitions

open Game.Core.Domain
open Game.Core.Domain.Competitions.Preset

module Competition =
    type RawRules =
        | Ok
    type CompetitionRulesConfig =
        | RawRules of RawRules: RawRules
        | Preset of preset: Preset

type CompetitionSettings = { Rules: CompetitionRulesConfig }

type Competition =
    { Hill: Hill
      InitialWind: WindMap
      CompetitionSettings: CompetitionSettings }