namespace App.Domain.Simulation

type Gate = Gate of int

type SimulationContext = {
    Gate: Gate
    Jumper: Jumper
    Hill: Hill
    HillPointsByGate: double
    HillPointsByMeter: double
    Wind: Wind
}

