namespace App.Application.Game.Gate;

public interface IGameStartingGateSelectorFactory
{
    Task<IGameStartingGateSelector> CreateForCompetition(IEnumerable<Domain.Competition.Jumper> jumpers,
        Domain.Competition.Hill hill, CancellationToken ct);
}