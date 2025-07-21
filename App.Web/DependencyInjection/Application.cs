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
            .AddScoped<ICommandHandler<Application.UseCase.Game.CreateQuickGame.Command, Domain.Game.Game>,
                Application.UseCase.Game.CreateQuickGame.Handler>();
        services
            .AddScoped<ICommandHandler<Application.UseCase.Game.EndDraftPhase.Command>,
                Application.UseCase.Game.EndDraftPhase.Handler>();
        services
            .AddScoped<ICommandHandler<Application.UseCase.Game.ChooseQuickGameHill.Command,
                Domain.GameWorld.HillModule.Id>, Application.UseCase.Game.ChooseQuickGameHill.Handler>();
        services
            .AddScoped<ICommandHandler<Application.UseCase.Game.QuickMatchmaking.FindOrCreate.Command, Guid>,
                Application.UseCase.Game.QuickMatchmaking.FindOrCreate.Handler>();
        services
            .AddScoped<ICommandHandler<Application.UseCase.Game.QuickMatchmaking.Join.Command,
                Domain.Matchmaking.ParticipantModule.Id>, Application.UseCase.Game.QuickMatchmaking.Join.Handler>();
        return services;
    }
}