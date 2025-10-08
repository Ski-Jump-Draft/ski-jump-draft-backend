using App.Domain.Simulation;

namespace App.Application.Game.GameWind;

public interface IGameWind
{
    Wind? Get(Guid gameId);
    void Set(Guid gameId, Wind value);
}