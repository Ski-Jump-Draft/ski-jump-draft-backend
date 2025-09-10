namespace App.Domain.Simulation

type IJudgesSimulator =
    abstract member Evaluate : context: JudgesSimulationContext -> Judges