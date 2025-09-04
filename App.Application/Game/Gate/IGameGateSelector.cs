namespace App.Application.Game.Gate;

public interface IGameGateSelector
{
    int Select(Domain.Game.Game game);
}