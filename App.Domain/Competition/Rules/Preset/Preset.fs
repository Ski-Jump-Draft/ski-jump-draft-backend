namespace App.Domain.Competition.Rules.Preset

open App.Domain.Competition
open App.Domain.Shared.Ids

module Preset =
    open App.Domain.Competition.Rules
    type Name = private Name of string

    module Name =
        let tryCreate (s: string) =
            if s.Length > 0 && s.Length < 40 then Some(Name s) else None

        let value (Name s) = s

    type Variant =
        | Classic of Classic
        | OneVsOneKo of OneVsOneKo
        | Custom of Rules.Raw
    type Type =
        | Individual
        | Team of Shared.TeamSize
        
type Preset =
    { Id: CompetitionRulesPresetId
      Name: Preset.Name
      Type: Preset.Type
      Variant: Preset.Variant }