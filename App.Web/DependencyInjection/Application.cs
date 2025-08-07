using App.Application.Commanding;

namespace App.Web.DependencyInjection;

public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services
            .AddScoped<ICommandHandler<Application.UseCase.Handlers.AdjustGate.Command>,
                Application.UseCase.Handlers.AdjustGate.Handler>()
            .AddScoped<ICommandHandler<Application.UseCase.Handlers.ContinuePreDraft.Command,
                    Domain.SimpleCompetition.CompetitionId>,
                Application.UseCase.Handlers.ContinuePreDraft.Handler>()
            .AddScoped<ICommandHandler<Application.UseCase.Handlers.EndCompetition.Command>,
                Application.UseCase.Handlers.EndCompetition.Handler>()
            .AddScoped<ICommandHandler<Application.UseCase.Handlers.EndDraft.Command>,
                Application.UseCase.Handlers.EndDraft.Handler>()
            .AddScoped<ICommandHandler<Application.UseCase.Handlers.EndGame.Command>,
                Application.UseCase.Handlers.EndGame.Handler>()
            .AddScoped<ICommandHandler<Application.UseCase.Handlers.EndPreDraft.Command>,
                Application.UseCase.Handlers.EndPreDraft.Handler>()
            .AddScoped<ICommandHandler<Application.UseCase.Handlers.JoinQuickMatchmaking.Command,
                    Application.UseCase.Handlers.JoinQuickMatchmaking.Result>,
                Application.UseCase.Handlers.JoinQuickMatchmaking.Handler>()
            .AddScoped<ICommandHandler<Application.UseCase.Handlers.LeaveMatchmaking.Command>,
                Application.UseCase.Handlers.LeaveMatchmaking.Handler>()
            .AddScoped<ICommandHandler<Application.UseCase.Handlers.PickSubjectInDraft.Command>,
                Application.UseCase.Handlers.PickSubjectInDraft.Handler>()
            .AddScoped<ICommandHandler<Application.UseCase.Handlers.SimulateJump.Command>,
                Application.UseCase.Handlers.SimulateJump.Handler>()
            .AddScoped<ICommandHandler<Application.UseCase.Handlers.StartCompetition.Command,
                    Domain.SimpleCompetition.CompetitionId>,
                Application.UseCase.Handlers.StartCompetition.Handler>()
            .AddScoped<ICommandHandler<Application.UseCase.Handlers.StartDraft.Command>,
                Application.UseCase.Handlers.StartDraft.Handler>()
            .AddScoped<ICommandHandler<Application.UseCase.Handlers.StartGame.Command, Domain.Game.Id.Id>,
                Application.UseCase.Handlers.StartGame.Handler>()
            .AddScoped<ICommandHandler<Application.UseCase.Handlers.StartPreDraft.Command,
                    Application.UseCase.Handlers.StartPreDraft.Result>,
                Application.UseCase.Handlers.StartPreDraft.Handler>()
            .AddScoped<ICommandHandler<Application.UseCase.Handlers.TryUpdateHillRecords.Command>,
                Application.UseCase.Handlers.TryUpdateHillRecords.Handler>();


        return services;
    }
}