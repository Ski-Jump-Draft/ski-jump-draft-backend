using App.Application.Commanding;
using App.Domain.Game;
using App.Web;
using App.Web.DependencyInjection;
using App.Web.Notifiers.SseHub;
using App.Web.SignalR.Hub;
using Microsoft.AspNetCore.Mvc;
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

DotNetEnv.Env.Load(".env");

builder.Services.AddSignalR();

const Mode mode = Mode.Online;
builder.Services.InjectDependencies(mode);

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
        [FromServices] App.Application.Utility.IMyLogger myLogger,
        CancellationToken ct) =>
    {
        var command = new App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.Command(nick);
        var (matchmakingId, correctedNick, playerId) = await commandBus
            .SendAsync<App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.Command,
                App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.Result>(command, ct);
        return Results.Ok(new { MatchmakingId = matchmakingId, CorrectedNick = correctedNick, PlayerId = playerId });
    });

app.MapDelete("/matchmaking/leave",
    async (Guid matchmakingId, Guid playerId, [FromServices] ICommandBus commandBus,
        CancellationToken ct) =>
    {
        var command = new App.Application.UseCase.Matchmaking.LeaveMatchmaking.Command(matchmakingId, playerId);
        await commandBus
            .SendAsync(command, ct);
        return Results.NoContent();
    });

app.MapGet("/matchmaking",
    async (Guid matchmakingId, [FromServices] ICommandBus commandBus,
        CancellationToken ct) =>
    {
        var command = new App.Application.UseCase.Matchmaking.GetMatchmaking.Command(matchmakingId);
        try
        {
            var result = await commandBus
                .SendAsync<App.Application.UseCase.Matchmaking.GetMatchmaking.Command,
                    App.Application.UseCase.Matchmaking.GetMatchmaking.Result>(command, ct);
            return Results.Ok(result);
        }
        catch (Exception)
        {
            return Results.Problem("Could not join");
        }
    });

app.MapGet("/game/{gameId:guid}/leave",
    async (Guid gameId, Guid playerId, [FromServices] ICommandBus commandBus,
        [FromServices] App.Domain.Game.IGames repo, [FromServices] App.Application.Utility.IMyLogger myLogger,
        CancellationToken ct) =>
    {
        var command = new App.Application.UseCase.Game.LeaveGame.Command(gameId, playerId);
        try
        {
            var result = await commandBus
                .SendAsync<App.Application.UseCase.Game.LeaveGame.Command,
                    App.Application.UseCase.Game.LeaveGame.Result>(command, ct);
            var hasLeft = result.HasLeft;
            return !hasLeft ? Results.NotFound() : Results.Ok(result);
        }
        catch (Exception e)
        {
            myLogger.Error($"Error during leaving a game: {e.Message} (gameId: {gameId}, playerId: {playerId})");
            return Results.InternalServerError();
        }
    });


app.MapPost("/game/{gameId:guid}/pick",
    async (Guid gameId, Guid playerId, Guid jumperId, [FromServices] ICommandBus commandBus,
        [FromServices] App.Domain.Game.IGames repo, [FromServices] App.Application.Utility.IMyLogger myLogger,
        CancellationToken ct) =>
    {
        var command = new App.Application.UseCase.Game.PickJumper.Command(gameId, playerId, jumperId);
        try
        {
            var pickResult = await commandBus
                .SendAsync<App.Application.UseCase.Game.PickJumper.Command,
                    App.Application.UseCase.Game.PickJumper.Result>(command, ct);
            return Results.Ok();
        }
        catch (App.Application.UseCase.Game.PickJumper.JumperTakenException)
        {
            return Results.Conflict();
        }
        catch (App.Application.UseCase.Game.PickJumper.NotYourTurnException)
        {
            return Results.Forbid();
        }
        catch (Exception e)
        {
            myLogger.Error($"Error during picking a jumper: {e.Message} (gameId: {gameId}, playerId: {playerId
            }, jumperId: {jumperId})");
            return Results.InternalServerError();
        }
    });

app.MapHub<GameHub>("/game/hub");

app.UseRouting();

if (mode == Mode.Offline)
{
    await OfflineTests.InitializeOfflineTest(app.Services,
        app.Services.GetRequiredService<App.Application.Utility.IMyLogger>());
}

app.Run();