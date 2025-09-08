using App.Application.Acl;
using App.Application.Exceptions;
using App.Application.Extensions;
using App.Domain.GameWorld;

namespace App.Web.DependencyInjection.Shared;

public static class Acl
{
    public static IServiceCollection AddAcl(this IServiceCollection services)
    {
        services.AddSingleton<IGameJumperAcl, App.Infrastructure.Acl.GameJumpers.InMemory>();
        services.AddSingleton<ICompetitionJumperAcl, App.Infrastructure.Acl.CompetitionJumper.InMemory>();
        services.AddSingleton<ICompetitionHillAcl, App.Infrastructure.Acl.CompetitionHill.InMemory>();
        
        services
            .AddSingleton<
                Func<App.Domain.Competition.JumperId, CancellationToken, Task<App.Domain.GameWorld.Jumper>>>(sp =>
            {
                var competitionJumperAcl = sp.GetRequiredService<ICompetitionJumperAcl>();
                var gameJumperAcl = sp.GetRequiredService<IGameJumperAcl>();
                var jumpers = sp.GetRequiredService<IJumpers>();

                return async (competitionJumperId, ct) =>
                {
                    var gameJumper = competitionJumperAcl.GetGameJumper(competitionJumperId.Item);
                    var gameWorldJumper = gameJumperAcl.GetGameWorldJumper(gameJumper.Id);
                    return await jumpers.GetById(JumperId.NewJumperId(gameWorldJumper.Id), ct)
                        .AwaitOrWrap(_ => new IdNotFoundException(gameWorldJumper.Id));
                };
            });
        return services;
    }
}