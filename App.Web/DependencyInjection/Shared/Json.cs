using System.Text.Json;

namespace App.Web.DependencyInjection.Shared;

public static class Json
{
    public static IServiceCollection AddJson(this IServiceCollection services)
    {
        services.Configure<JsonSerializerOptions>(options =>
        {
            options.IncludeFields = false; // domyślnie
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            // Właściwości get-only powinny być serializowane domyślnie
        });
        return services;
    }
}