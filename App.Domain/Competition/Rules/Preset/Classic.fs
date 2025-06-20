namespace App.Domain.Competition.Rules.Preset

module Classic =
    type ExAequoPolicy =
        | NoTie
        | AddOneEachTie
        | CoverAll

    [<Struct>]
    type Limit = private Limit of int with
        static member TryCreate(v:int) : Limit option =
            if v > 0 then Some (Limit v) else None
        static member Value(Limit v) = v

    type LimitPolicy =
        | Soft      of Limit
        | Exact     of Limit
        | Unlimited

    type RoundConfig = {
        Limit : LimitPolicy
        IsKo  : bool
    }

open Classic
type Classic = private {
    Rounds : RoundConfig list
    Policy : ExAequoPolicy
} with
    static member TryCreate(rounds:RoundConfig list, policy:ExAequoPolicy) : Classic option =
        match List.rev rounds with
        | { IsKo = true } :: _ -> None
        | _                   -> Some { Rounds = rounds; Policy = policy }
