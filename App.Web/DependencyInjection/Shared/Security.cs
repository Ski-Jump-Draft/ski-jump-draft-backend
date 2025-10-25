using App.Web.Security;

namespace App.Web.DependencyInjection.Shared;

public static class Security
{
    public static IServiceCollection AddSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        // Read secret from configuration: Web:PlayerTokenSecret or PLAYER_TOKEN_SECRET env
        var secret = configuration["Web:PlayerTokenSecret"] ?? configuration["PLAYER_TOKEN_SECRET"] ?? string.Empty;
        services.AddSingleton<IPlayerTokenService>(_ => new HmacPlayerTokenService(secret));
        services.AddSingleton<IGamePlayerMappingStore, InMemoryGamePlayerMappingStore>();
        return services;
    }
}
