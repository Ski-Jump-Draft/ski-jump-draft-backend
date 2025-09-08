using App.Application.Commanding;
using App.Application.Matchmaking;

namespace App.Web.DependencyInjection.Shared;

public static class Application
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services
            .AddSingleton<
                ICommandHandler<App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.Command,
                    App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.Result>,
                App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.Handler>();
        services
            .AddSingleton<
                ICommandHandler<App.Application.UseCase.Matchmaking.EndMatchmaking.Command,
                    App.Application.UseCase.Matchmaking.EndMatchmaking.Result>,
                App.Application.UseCase.Matchmaking.EndMatchmaking.Handler>();
        services
            .AddSingleton<
                ICommandHandler<App.Application.UseCase.Matchmaking.LeaveMatchmaking.Command>,
                App.Application.UseCase.Matchmaking.LeaveMatchmaking.Handler>();
        services
            .AddSingleton<
                ICommandHandler<App.Application.UseCase.Matchmaking.GetMatchmaking.Command,
                    App.Application.UseCase.Matchmaking.GetMatchmaking.Result>,
                App.Application.UseCase.Matchmaking.GetMatchmaking.Handler>();
        services
            .AddSingleton<
                ICommandHandler<App.Application.UseCase.Game.StartGame.Command,
                    App.Application.UseCase.Game.StartGame.Result>,
                App.Application.UseCase.Game.StartGame.Handler>();
        services
            .AddSingleton<
                ICommandHandler<App.Application.UseCase.Game.StartPreDraft.Command,
                    App.Application.UseCase.Game.StartPreDraft.Result>,
                App.Application.UseCase.Game.StartPreDraft.Handler>();
        services
            .AddSingleton<
                ICommandHandler<App.Application.UseCase.Game.StartNextPreDraftCompetition.Command,
                    App.Application.UseCase.Game.StartNextPreDraftCompetition.Result>,
                App.Application.UseCase.Game.StartNextPreDraftCompetition.Handler>();
        services
            .AddSingleton<
                ICommandHandler<App.Application.UseCase.Game.StartDraft.Command,
                    App.Application.UseCase.Game.StartDraft.Result>,
                App.Application.UseCase.Game.StartDraft.Handler>();
        services
            .AddSingleton<
                ICommandHandler<App.Application.UseCase.Game.PickJumper.Command,
                    App.Application.UseCase.Game.PickJumper.Result>,
                App.Application.UseCase.Game.PickJumper.Handler>();
        services
            .AddSingleton<
                ICommandHandler<App.Application.UseCase.Game.PassPick.Command,
                    App.Application.UseCase.Game.PassPick.Result>,
                App.Application.UseCase.Game.PassPick.Handler>();
        services
            .AddSingleton<
                ICommandHandler<App.Application.UseCase.Game.SimulateJump.Command,
                    App.Application.UseCase.Game.SimulateJump.Result>,
                App.Application.UseCase.Game.SimulateJump.Handler>();
        services
            .AddSingleton<
                ICommandHandler<App.Application.UseCase.Game.StartMainCompetition.Command,
                    App.Application.UseCase.Game.StartMainCompetition.Result>,
                App.Application.UseCase.Game.StartMainCompetition.Handler>();
        services
            .AddSingleton<
                ICommandHandler<App.Application.UseCase.Game.LeaveGame.Command,
                    App.Application.UseCase.Game.LeaveGame.Result>,
                App.Application.UseCase.Game.LeaveGame.Handler>();
        services
            .AddSingleton<
                ICommandHandler<App.Application.UseCase.Game.EndGame.Command,
                    App.Application.UseCase.Game.EndGame.Result>,
                App.Application.UseCase.Game.EndGame.Handler>();

        services.AddSingleton<IMatchmakingSchedule, App.Infrastructure.Matchmaking.Schedule.InMemory>();
        return services;
    }
}