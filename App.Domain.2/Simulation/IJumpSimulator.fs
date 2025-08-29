namespace App.Domain._2.Simulation

type IJumpSimulator =
    abstract member Simulate : context: SimulationContext -> Jump