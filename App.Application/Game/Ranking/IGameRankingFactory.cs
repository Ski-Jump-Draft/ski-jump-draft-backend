namespace App.Application.Game.Ranking;

public interface IGameRankingFactory
{
    Task<Domain.Game.Ranking> Create(Domain.Game.Game game, CancellationToken ct);
}