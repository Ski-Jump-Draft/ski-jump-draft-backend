module App.Domain.Competition.Jump.Gate

open App.Domain.Competition

type Gate = Gate of int

module CoachStatus =
    module LoweredGate =
        type Count = private Count of int

        module Count =
            type Error = ZeroOrLess of Value: int

            let tryCreate (v: int) =
                if v > 0 then Ok(Count v) else Error(ZeroOrLess v)
                
            let value (Count v) = v


type CoachStatus =
    | None
    | LoweredGate of Count: CoachStatus.LoweredGate.Count

type GateStatus =
    | None
    | Some of StartGate: Gate * CurrentGate: Gate * CoachStatus: CoachStatus

