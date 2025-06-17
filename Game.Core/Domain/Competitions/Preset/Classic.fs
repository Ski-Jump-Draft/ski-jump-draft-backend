namespace Game.Core.Domain.Competitions.Preset.Classic

type ExAequoPolicy =
  | NoTie
  | AddOneEachTie
  | CoverAll

[<Struct>]
type Limit = private Limit of int
module Limit =
  let tryCreate v = if v > 0 then Some (Limit v) else None
  let value (Limit v) = v

type LimitPolicy =
  | Soft      of Limit
  | Exact     of Limit
  | Unlimited

type RoundConfig = {
  Limit   : LimitPolicy
  IsKo    : bool
}

type ClassicPreset = private {
  Rounds : RoundConfig list
  Policy : ExAequoPolicy
}
module ClassicPreset =
  let tryCreate (rounds:RoundConfig list) (policy:ExAequoPolicy) =
    match List.rev rounds with
    | { IsKo = true } :: _ -> None
    | _                   -> Some { Rounds = rounds; Policy = policy }
