namespace App.Domain.Competition

type Gate = Gate of int

module Gate =
    let value (Gate v) = v

type GatesLoweredByCoach = GatesLoweredByCoach of int

module GatesLoweredByCoach =
    let value (GatesLoweredByCoach v) = v

type GateChange =
    | Increase of uint
    | Reduction of uint

    static member CreateIncrease(by: uint) =
        if by < 1u then None else Some(Increase by)

    static member CreateReduction(by: uint) =
        if by < 1u then None else Some(Reduction by)

    member this.ToInt() : int =
        match this with
        | Increase value -> (int) value
        | Reduction value -> -(int) value

type GateState =
    { Starting: Gate
      CurrentJury: Gate
      CoachChange: GateChange option }

    member this.CurrentReal: Gate =
        let current = Gate.value this.CurrentJury

        let coachReduction =
            match this.CoachChange with
            | Some coachChange ->
                match coachChange with
                | Increase _ -> invalidOp "Coach cannot increase the gate. It should not happen. Please report the bug"
                | Reduction by -> int (by)
            | None -> 0

        let realGate = current - coachReduction
        Gate realGate
