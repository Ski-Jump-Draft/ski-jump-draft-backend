using App.Domain.Game;

namespace App.Web.DependencyInjection;

public static class GameRankingDependencyInjection
{
    public static IServiceCollection AddGameRanking(
        this IServiceCollection services)
    {
        services.AddSingleton<Ranking.IGameRankingFactory, Application.Factory.Impl.GameRanking.Classic>();
        return services;
    }
}