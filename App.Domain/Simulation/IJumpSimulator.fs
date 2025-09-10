namespace App.Domain.Simulation

type IJumpSimulator =
    abstract member Simulate : context: SimulationContext -> Jump