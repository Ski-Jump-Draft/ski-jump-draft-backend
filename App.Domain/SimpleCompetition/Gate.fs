namespace App.Domain.SimpleCompetition

type GateChange =
    | Increase of uint
    | Reduction of uint

    static member CreateIncrease(by: uint) =
        if by < 1u then None else Some(Increase by)

    static member CreateReduction(by: uint) =
        if by < 1u then None else Some(Reduction by)

type GateState =
    { Starting: Jump.Gate
      CurrentJury: Jump.Gate
      CoachChange: GateChange option }

    member this.CurrentReal =
        let current = Jump.GateModule.value this.CurrentReal

        let coachReduction =
            match this.CoachChange with
            | Some coahcChange ->
                match coahcChange with
                | Increase _ -> invalidOp "Coach cannot increase the gate. It should not happen. Please report the bug"
                | Reduction by -> int (by)
            | None -> 0

        let realGate = current - coachReduction
        Jump.Gate realGate
