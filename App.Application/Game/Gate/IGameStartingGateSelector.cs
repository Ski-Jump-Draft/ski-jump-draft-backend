namespace App.Application.Game.Gate;

public interface IGameStartingGateSelector
{
    int Select(GameStartingGateSelectorContext context);
}

public record GameStartingGateSelectorContext();