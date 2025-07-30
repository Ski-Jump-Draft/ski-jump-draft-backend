using App.Application.Abstractions;

namespace App.Web.DependencyInjection;

public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services
            .AddScoped<ICommandHandler<Application.UseCase.Game.Matchmaking.Leave.Command,
                Domain.Matchmaking.ParticipantModule.Id>, Application.UseCase.Game.Matchmaking.Leave.Handler>();
        services
            .AddScoped<ICommandHandler<Application.UseCase.Game.CreateCompetitionEngine.Command,
                Domain.Competition.Engine.Id>, Application.UseCase.Game.CreateCompetitionEngine.Handler>();
        services
            .AddScoped<ICommandHandler<Application.UseCase.Game.QuickGame.Create.Command, Domain.Game.Game>,
                Application.UseCase.Game.QuickGame.Create.Handler>();
        services
            .AddScoped<ICommandHandler<Application.UseCase.Game.EndDraftPhase.Command>,
                Application.UseCase.Game.EndDraftPhase.Handler>();
        services
            .AddScoped<ICommandHandler<Application.UseCase.Game.QuickGame.ChooseHill.Command,
                Domain.GameWorld.HillId>, Application.UseCase.Game.QuickGame.ChooseHill.Handler>();
        services
            .AddScoped<ICommandHandler<Application.UseCase.Game.QuickGame.FindOrCreateMatchmaking.Command, Guid>,
                Application.UseCase.Game.QuickGame.FindOrCreateMatchmaking.Handler>();
        services
            .AddScoped<ICommandHandler<Application.UseCase.Game.QuickGame.JoinMatchmaking.Command,
                Domain.Matchmaking.ParticipantModule.Id>, Application.UseCase.Game.QuickGame.JoinMatchmaking.Handler>();
        return services;
    }
}