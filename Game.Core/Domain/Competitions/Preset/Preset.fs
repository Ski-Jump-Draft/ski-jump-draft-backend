namespace Game.Core.Domain.Competitions.Preset

open Game.Core.Domain.Ids
open Game.Core.Domain.Competitions.Preset.Classic

type PresetName = private PresetName of string
module PresetName =
  let tryCreate s = if s <> "" then Some (PresetName s) else None
  let value (PresetName s) = s

type RawRules = { Test: bool }

type Variant =
  | Classic  of ClassicPreset
  | FullKo
  | Custom   of RawRules

type Preset = {
  Id      : CompetitionRulesPresetId
  Name    : PresetName
  Variant : Variant
}
