using App;
using App.Application._2.Commanding;
using App.Application._2.Matchmaking;
using App.Application._2.Messaging.Notifiers;
using App.Application._2.Utility;
using App.Domain._2.Matchmaking;
using App.Infrastructure._2.Utility.Logger;
using App.Web._2.MockedFlow;
using App.Web._2.Notifiers.SseHub;
using Microsoft.AspNetCore.Mvc;
using ILogger = Microsoft.Extensions.Logging.ILogger;

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

builder.Services.AddSingleton<App.Domain._2.Matchmaking.Settings>(sp =>
    Settings.Create(SettingsModule.MinPlayersModule.create(3).Value,
        SettingsModule.MaxPlayersModule.create(5).Value).ResultValue);

builder.Services.AddSingleton<App.Application._2.Utility.ILogger, App.Infrastructure._2.Utility.Logger.Dotnet>();

builder.Services.AddSingleton<App.Web._2.Notifiers.SseHub.ISseHub, App.Web._2.Notifiers.SseHub.Default>();
builder.Services.AddSingleton<IMatchmakingNotifier, App.Web._2.Notifiers.Matchmaking.Sse>();
builder.Services.AddSingleton<IMatchmakings, App.Infrastructure._2.Repository.Matchmaking.InMemory>();
builder.Services.AddSingleton<IMatchmakingSchedule, App.Infrastructure._2.Matchmaking.Schedule.InMemory>();

builder.Services.AddHostedService<BotJoiner>();

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
    async (string nick, [FromServices] ICommandBus commandBus, [FromServices] IMatchmakings repo,
        [FromServices] App.Application._2.Utility.ILogger logger,
        CancellationToken ct) =>
    {
        var command = new App.Application._2.UseCase.Matchmaking.JoinQuickMatchmaking.Command(nick);
        var (matchmakingId, correctedNick, playerId) = await commandBus
            .SendAsync<App.Application._2.UseCase.Matchmaking.JoinQuickMatchmaking.Command,
                App.Application._2.UseCase.Matchmaking.JoinQuickMatchmaking.Result>(command, ct);
        return Results.Ok(new { MatchmakingId = matchmakingId, CorrectedNick = correctedNick, PlayerId = playerId });
    });

app.MapDelete("/matchmaking/leave",
    async (Guid matchmakingId, Guid playerId, [FromServices] ICommandBus commandBus, [FromServices] IMatchmakings repo,
        CancellationToken ct) =>
    {
        var command = new App.Application._2.UseCase.Matchmaking.LeaveMatchmaking.Command(matchmakingId, playerId);
        await commandBus
            .SendAsync(command, ct);
        return Results.NoContent();
    });

app.MapGet("/matchmaking",
    async (Guid matchmakingId, [FromServices] ICommandBus commandBus, [FromServices] IMatchmakings repo,
        CancellationToken ct) =>
    {
        var command = new App.Application._2.UseCase.Matchmaking.GetMatchmaking.Command(matchmakingId);
        var result = await commandBus
            .SendAsync<App.Application._2.UseCase.Matchmaking.GetMatchmaking.Command,
                App.Application._2.UseCase.Matchmaking.GetMatchmaking.Result>(command, ct);
        return Results.Ok(result);
    });

app.Run();