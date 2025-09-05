namespace App.Application.Game.Gate;

public interface IGameStartingGateSelector
{
    int Select(GameStartingGateSelectorContext context);
}

public record GameStartingGateSelectorContext(IEnumerable<Domain.GameWorld.Jumper> Jumpers, Domain.GameWorld.Hill Hill);