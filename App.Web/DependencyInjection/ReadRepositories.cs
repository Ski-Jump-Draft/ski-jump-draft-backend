namespace App.Web.DependencyInjection;

public static class ReadRepositoriesDependencyInjection
{
    public static IServiceCollection AddReadRepositories(
        this IServiceCollection services,
        IConfiguration config)
    {
        return services;
    }
}