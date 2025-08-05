using App.Application.Abstractions;
using App.Application.Abstractions.Mappers;
using App.Application.ReadModel.Projection;
using App.Application.UseCase.Helper;
using App.Application.UseCase.Helper.Impl.QuickGameHillSelector;
using App.Infrastructure.Globals;

namespace App.Web.DependencyInjection;

public static class GlobalsDependencyInjection
{
    public static IServiceCollection AddGlboals(
        this IServiceCollection services)
    {

        return services;
    }
}