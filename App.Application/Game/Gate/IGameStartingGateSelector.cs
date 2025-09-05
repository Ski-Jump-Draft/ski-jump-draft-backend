namespace App.Application.Game.Gate;

public interface IGameStartingGateSelector
{
    int Select(GameStartingGateSelectorContext context);
}

public record GameStartingGateSelectorContext(IEnumerable<Domain.Simulation.Jumper> Jumpers, Domain.Simulation.Hill Hill);