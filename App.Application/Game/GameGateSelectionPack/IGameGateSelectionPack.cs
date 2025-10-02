using App.Application.Game.Gate;

namespace App.Application.Game.GameGateSelectionPack;

public record GameGateSelectionPack(
    IStartingGateSelector StartingGateSelector);

public interface IGameGateSelectionPack
{
    Task<GameGateSelectionPack> GetForCompetition(Guid gameId, IEnumerable<Domain.Competition.Jumper> jumpers, Domain.Competition.Hill hill, CancellationToken ct);
}