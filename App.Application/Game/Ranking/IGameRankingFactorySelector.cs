namespace App.Application.Game.Ranking;

public interface IGameRankingFactorySelector
{
    IGameRankingFactory Select(Domain.Game.RankingPolicy rankingPolicy);
}