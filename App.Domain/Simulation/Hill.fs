namespace App.Domain.Simulation

module Hill =
    type KPoint = private KPoint of double

    module KPoint =
        let tryCreate (v: double) =
            if v >= 0.0 then Some(KPoint v) else Option.None

        let value (KPoint v) = v

    type HsPoint = private HsPoint of double

    module HsPoint =
        let tryCreate (v: double) =
            if v >= 0.0 then Some(HsPoint v) else Option.None

        let value (HsPoint v) = v

    type GatePoints = private GatePoints of double

open Hill
type Hill = {
    KPoint: KPoint
    HsPoint: HsPoint
    SimulationData: HillSimulationData
}
and HillSimulationData = {
    RealHs: HsPoint
}