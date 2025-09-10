namespace App.Domain.Simulation

type Gate = Gate of int

type SimulationContext = {
    Gate: Gate
    Jumper: Jumper
    Hill: Hill
    Wind: Wind
}

type JudgesSimulationContext = {
    Jump: Jump
    Gate: Gate
    Jumper: Jumper
    Hill: Hill
    Wind: Wind
}
