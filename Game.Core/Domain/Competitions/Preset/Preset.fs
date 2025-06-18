namespace Game.Core.Domain.Competitions

open Game.Core.Domain.Competitions.Preset
open Game.Core.Domain.Competitions.RawRules
open Game.Core.Domain.Competitions.SharedTypes
open Game.Core.Domain.Shared.Ids

module RulesPreset =
    type Name = private Name of string

    module Name =
        let tryCreate (s: string) =
            if s.Length > 0 && s.Length < 40 then Some(Name s) else None

        let value (Name s) = s

    type Variant =
        | Classic of Classic.Definition
        | OneVsOneKo of OneVsOneKo.Definition
        | Custom of RawRules.Definition
    type Type =
        | Individual
        | Team of TeamSize

    type Definition =
        { Id: CompetitionRulesPresetId
          Name: Name
          Type: Type
          Variant: Variant }