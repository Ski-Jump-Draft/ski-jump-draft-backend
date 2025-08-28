using App.Application._2.Acl;
using App.Application._2.Commanding;
using App.Application._2.Matchmaking;
using App.Application._2.Messaging.Notifiers;
using App.Application._2.Policy.GameHillSelector;
using App.Application._2.Policy.GameJumpersSelector;
using App.Application._2.Utility;
using App.Web._2.MockedFlow;
using App.Web._2.Notifiers.Game;
using App.Web._2.Notifiers.SseHub;
using App.Web._2.SignalR.Hub;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FSharp.Collections;
using Results = Microsoft.AspNetCore.Http.Results;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("http://localhost:3000")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials(); // jeśli będziesz używał cookies/sse
        });
});

builder.Services.AddSignalR();

builder.Services.AddSingleton<IGuid, App.Infrastructure._2.Utility.Guid.System>();
builder.Services.AddSingleton<IClock, App.Infrastructure._2.Utility.Clock.System>();
// builder.Services.AddSingleton<IRandom, >();
builder.Services.AddSingleton<IJson, App.Infrastructure._2.Utility.Json.System>();
builder.Services.AddSingleton<ICommandBus, App.Infrastructure._2.Commanding.CommandBus.InMemory>();
builder.Services.AddSingleton<IScheduler, App.Infrastructure._2.Commanding.Scheduler.InMemory>();

builder.Services
    .AddSingleton<
        ICommandHandler<App.Application._2.UseCase.Matchmaking.JoinQuickMatchmaking.Command,
            App.Application._2.UseCase.Matchmaking.JoinQuickMatchmaking.Result>,
        App.Application._2.UseCase.Matchmaking.JoinQuickMatchmaking.Handler>();
builder.Services
    .AddSingleton<
        ICommandHandler<App.Application._2.UseCase.Matchmaking.EndMatchmaking.Command,
            App.Application._2.UseCase.Matchmaking.EndMatchmaking.Result>,
        App.Application._2.UseCase.Matchmaking.EndMatchmaking.Handler>();
builder.Services
    .AddSingleton<
        ICommandHandler<App.Application._2.UseCase.Matchmaking.LeaveMatchmaking.Command>,
        App.Application._2.UseCase.Matchmaking.LeaveMatchmaking.Handler>();
builder.Services
    .AddSingleton<
        ICommandHandler<App.Application._2.UseCase.Matchmaking.GetMatchmaking.Command,
            App.Application._2.UseCase.Matchmaking.GetMatchmaking.Result>,
        App.Application._2.UseCase.Matchmaking.GetMatchmaking.Handler>();
builder.Services
    .AddSingleton<
        ICommandHandler<App.Application._2.UseCase.Game.StartGame.Command,
            App.Application._2.UseCase.Game.StartGame.Result>,
        App.Application._2.UseCase.Game.StartGame.Handler>();
builder.Services
    .AddSingleton<
        ICommandHandler<App.Application._2.UseCase.Game.StartPreDraft.Command,
            App.Application._2.UseCase.Game.StartPreDraft.Result>,
        App.Application._2.UseCase.Game.StartPreDraft.Handler>();
builder.Services
    .AddSingleton<
        ICommandHandler<App.Application._2.UseCase.Game.LeaveGame.Command,
            App.Application._2.UseCase.Game.LeaveGame.Result>,
        App.Application._2.UseCase.Game.LeaveGame.Handler>();


builder.Services.AddSingleton<App.Domain._2.Matchmaking.Settings>(sp =>
    App.Domain._2.Matchmaking.Settings.Create(App.Domain._2.Matchmaking.SettingsModule.MinPlayersModule.create(3).Value,
        App.Domain._2.Matchmaking.SettingsModule.MaxPlayersModule.create(5).Value).ResultValue);

builder.Services.AddSingleton<App.Domain._2.Game.Settings>(sp =>
{
    var preDraftCompetitionSettings = App.Domain._2.Competition.Settings.Create(ListModule.OfSeq(new[]
        {
            new App.Domain._2.Competition.RoundSettings(App.Domain._2.Competition.RoundLimit.NoneLimit, false, false)
        }))
        .ResultValue;
    var preDraftSettings = App.Domain._2.Game.PreDraftSettings.Create(ListModule.OfSeq(
            new List<App.Domain._2.Competition.Settings> { preDraftCompetitionSettings, preDraftCompetitionSettings }))
        .Value;
    var mainCompetitionSettings = App.Domain._2.Competition.Settings.Create(ListModule.OfSeq(new[]
    {
        new App.Domain._2.Competition.RoundSettings(
            App.Domain._2.Competition.RoundLimit.NewSoft(App.Domain._2.Competition.RoundLimitValueModule.tryCreate(50)
                .ResultValue), false, false),
        new App.Domain._2.Competition.RoundSettings(
            App.Domain._2.Competition.RoundLimit.NewSoft(App.Domain._2.Competition.RoundLimitValueModule.tryCreate(30)
                .ResultValue), true, false)
    })).ResultValue;
    var draftSettings = new App.Domain._2.Game.DraftModule.Settings(
        App.Domain._2.Game.DraftModule.SettingsModule.TargetPicksModule.create(4).Value,
        App.Domain._2.Game.DraftModule.SettingsModule.MaxPicksModule.create(4).Value,
        App.Domain._2.Game.DraftModule.SettingsModule.UniqueJumpersPolicy.Unique,
        App.Domain._2.Game.DraftModule.SettingsModule.Order.Snake,
        App.Domain._2.Game.DraftModule.SettingsModule.TimeoutPolicy.NewTimeoutAfter(TimeSpan.FromSeconds(15)));
    return new App.Domain._2.Game.Settings(preDraftSettings, draftSettings, mainCompetitionSettings,
        App.Domain._2.Game.RankingPolicy.Classic);
});


builder.Services.AddSingleton<App.Application._2.Utility.IMyLogger, App.Infrastructure._2.Utility.Logger.Dotnet>();

builder.Services
    .AddSingleton<App.Domain._2.Matchmaking.IMatchmakings, App.Infrastructure._2.Repository.Matchmaking.InMemory>();
builder.Services.AddSingleton<IMatchmakingSchedule, App.Infrastructure._2.Matchmaking.Schedule.InMemory>();

builder.Services.AddSingleton<App.Domain._2.Game.IGames, App.Infrastructure._2.Repository.Game.InMemory>();

builder.Services.AddHostedService<BotJoiner>();

// SSE, WS
builder.Services.AddSingleton<App.Web._2.Notifiers.SseHub.ISseHub, App.Web._2.Notifiers.SseHub.Default>();
builder.Services.AddSingleton<IMatchmakingNotifier, App.Web._2.Notifiers.Matchmaking.Sse>();
builder.Services.AddSingleton<IGameNotifier, SignalRGameNotifier>();

builder.Services.AddSingleton<IGameHillSelector, App.Application._2.Policy.GameHillSelector.Mocked>();
builder.Services.AddSingleton<IGameJumpersSelector, App.Application._2.Policy.GameJumpersSelector.Mocked>();

builder.Services.AddSingleton<IGameJumperAcl, App.Infrastructure._2.Acl.GameJumpers.InMemory>();
builder.Services.AddSingleton<ICompetitionJumperAcl, App.Infrastructure._2.Acl.CompetitionJumpers.InMemory>();

var app = builder.Build();
app.UseCors("AllowFrontend");
// app.UseHttpsRedirection();

app.MapGet("/matchmaking/{matchmakingId:guid}/stream",
    async (Guid matchmakingId, HttpContext ctx, ISseHub hub) =>
    {
        hub.Subscribe(matchmakingId, ctx.Response, ctx.RequestAborted);
        await Task.Delay(-1, ctx.RequestAborted);
    });

app.MapPost("/matchmaking/join",
    async (string nick, [FromServices] ICommandBus commandBus,
        [FromServices] App.Application._2.Utility.IMyLogger myLogger,
        CancellationToken ct) =>
    {
        var command = new App.Application._2.UseCase.Matchmaking.JoinQuickMatchmaking.Command(nick);
        var (matchmakingId, correctedNick, playerId) = await commandBus
            .SendAsync<App.Application._2.UseCase.Matchmaking.JoinQuickMatchmaking.Command,
                App.Application._2.UseCase.Matchmaking.JoinQuickMatchmaking.Result>(command, ct);
        return Results.Ok(new { MatchmakingId = matchmakingId, CorrectedNick = correctedNick, PlayerId = playerId });
    });

app.MapDelete("/matchmaking/leave",
    async (Guid matchmakingId, Guid playerId, [FromServices] ICommandBus commandBus,
        CancellationToken ct) =>
    {
        var command = new App.Application._2.UseCase.Matchmaking.LeaveMatchmaking.Command(matchmakingId, playerId);
        await commandBus
            .SendAsync(command, ct);
        return Results.NoContent();
    });

app.MapGet("/matchmaking",
    async (Guid matchmakingId, [FromServices] ICommandBus commandBus,
        CancellationToken ct) =>
    {
        var command = new App.Application._2.UseCase.Matchmaking.GetMatchmaking.Command(matchmakingId);
        try
        {
            var result = await commandBus
                .SendAsync<App.Application._2.UseCase.Matchmaking.GetMatchmaking.Command,
                    App.Application._2.UseCase.Matchmaking.GetMatchmaking.Result>(command, ct);
            return Results.Ok(result);
        }
        catch (Exception e)
        {
            return Results.Problem("Could not join");
        }
    });

app.MapGet("/game/{gameId:guid}/leave",
    async (Guid gameId, Guid playerId, [FromServices] ICommandBus commandBus,
        [FromServices] App.Domain._2.Game.IGames repo, [FromServices] App.Application._2.Utility.IMyLogger myLogger,
        CancellationToken ct) =>
    {
        var command = new App.Application._2.UseCase.Game.LeaveGame.Command(gameId, playerId);
        try
        {
            var result = await commandBus
                .SendAsync<App.Application._2.UseCase.Game.LeaveGame.Command,
                    App.Application._2.UseCase.Game.LeaveGame.Result>(command, ct);
            var hasLeft = result.HasLeft;
            return !hasLeft ? Results.NotFound() : Results.Ok(result);
        }
        catch (Exception e)
        {
            myLogger.Error($"Error during leaving a game: {e.Message} (gameId: {gameId}, playerId: {playerId})");
            return Results.InternalServerError();
        }
    });

app.MapHub<GameHub>("/game/hub");

app.UseRouting();


app.Run();