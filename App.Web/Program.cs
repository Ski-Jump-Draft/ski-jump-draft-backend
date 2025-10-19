using App.Application.Commanding;
using App.Application.Utility;
using App.Domain.Game;
using App.Web;
using App.Web.DependencyInjection;
using App.Web.Notifiers.SseHub;
using App.Web.SignalR.Hub;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Serilog.Formatting.Compact;
using Results = Microsoft.AspNetCore.Http.Results;

var builder = WebApplication.CreateBuilder(args);

// if (!builder.Environment.IsDevelopment())
// {
//     Log.Logger = new LoggerConfiguration()
//         .Enrich.FromLogContext()
//         .WriteTo.Console(new RenderedCompactJsonFormatter()) // JSON per line
//         .CreateLogger();
//
//     builder.Logging.ClearProviders();
//     builder.Host.UseSerilog();
// }

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            var env = builder.Environment;
            if (env.IsDevelopment())
            {
                policy.WithOrigins(
                        "http://localhost:3000",
                        "https://ski-jump-draft.netlify.app",
                        "https://staging--ski-jump-draft.netlify.app")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            }
            else
            {
                // Production: only allow the public frontend origin
                policy.WithOrigins("https://ski-jump-draft.netlify.app")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            }
        });
});

DotNetEnv.Env.Load(".env");

builder.Services.AddSignalR();

const Mode mode = Mode.Online;

builder.Services.InjectDependencies(builder.Configuration, mode);

var app = builder.Build();
app.UseCors("AllowFrontend");

app.UseHttpsRedirection();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

// Basic security headers
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "no-referrer";
    headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
    // Minimal CSP suitable for API-only backend; adjust if serving pages
    headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; upgrade-insecure-requests";
    await next();
});

app.MapGet("/matchmaking/{matchmakingId:guid}/stream",
    async (Guid matchmakingId, HttpContext ctx, ISseHub hub) =>
    {
        ctx.Response.ContentType = "text/event-stream";
        ctx.Response.Headers["Cache-Control"] = "no-store";
        ctx.Response.Headers["X-Accel-Buffering"] = "no"; // disable buffering for some proxies
        hub.Subscribe(matchmakingId, ctx.Response, ctx.RequestAborted);
        await Task.Delay(-1, ctx.RequestAborted);
    });

app.MapPost("/matchmaking/join",
    async (string nick, [FromServices] ICommandBus commandBus,
        [FromServices] App.Application.Utility.IMyLogger myLogger,
        CancellationToken ct) =>
    {
        var command = new App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.Command(nick, IsBot: false);
        try
        {
            var (matchmakingId, correctedNick, playerId) = await commandBus
                .SendAsync<App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.Command,
                    App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.Result>(command, ct);

            return Results.Ok(new
                { MatchmakingId = matchmakingId, CorrectedNick = correctedNick, PlayerId = playerId });
        }
        catch (App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.MultipleGamesNotSupportedException)
        {
            return Results.Conflict(new
            {
                error = "MultipleGamesNotSupported",
                message =
                    "Nie udało się dołączyć do gry. Spróbuj ponownie za kilka minut. Pracujemy nad poprawą naszych serwerów, żeby utrzymywały wiele gier na raz."
            });
        }
        catch (App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.PlayerAlreadyJoinedException)
        {
            return Results.Conflict(new { error = "AlreadyJoined", message = "Gracz już dołączył." });
        }
        catch (App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.RoomIsFullException)
        {
            return Results.Conflict(new { error = "RoomIsFull", message = "Pokój jest pełny." });
        }
        catch (Exception error)
        {
            myLogger.Error($"Error during joining a matchmaking: {nick}. Error: {error.Message}, StackTrace: {
                error.StackTrace}");
            return Results.InternalServerError();
        }
    });

app.MapPost("/matchmaking/joinPremium",
    async (string nick, string password, [FromServices] ICommandBus commandBus,
        [FromServices] App.Application.Utility.IMyLogger myLogger,
        CancellationToken ct) =>
    {
        var command =
            new App.Application.UseCase.Matchmaking.JoinPremiumMatchmaking.Command(nick, Password: password,
                IsBot: false);
        try
        {
            var (matchmakingId, correctedNick, playerId) = await commandBus
                .SendAsync<App.Application.UseCase.Matchmaking.JoinPremiumMatchmaking.Command,
                    App.Application.UseCase.Matchmaking.JoinPremiumMatchmaking.Result>(command, ct);

            return Results.Ok(new
                { MatchmakingId = matchmakingId, CorrectedNick = correctedNick, PlayerId = playerId });
        }
        catch (App.Application.UseCase.Matchmaking.JoinPremiumMatchmaking.InvalidPasswordException)
        {
            return Results.Conflict(new
            {
                error = "InvalidPasswordException",
                message =
                    "Wpisano niepoprawne hasło dostępu do prywatnego pokoju."
            });
        }
        catch (App.Application.UseCase.Matchmaking.JoinPremiumMatchmaking.PlayerAlreadyJoinedException)
        {
            return Results.Conflict(new { error = "AlreadyJoined", message = "Gracz już dołączył." });
        }
        catch (App.Application.UseCase.Matchmaking.JoinPremiumMatchmaking.RoomIsFullException)
        {
            return Results.Conflict(new { error = "RoomIsFull", message = "Pokój jest pełny." });
        }
        catch (App.Application.UseCase.Matchmaking.JoinPremiumMatchmaking.PrivateServerInUse)
        {
            return Results.Conflict(new
                { error = "PrivateServerInUse", message = "Gra na tym serwerze jest już rozgrywana." });
        }
        catch (Exception error)
        {
            myLogger.Error($"Error during joining a matchmaking: {nick}. Error: {error.Message}, StackTrace: {
                error.StackTrace}");
            return Results.InternalServerError();
        }
    });

app.MapDelete("/matchmaking/leave",
    async (Guid matchmakingId, Guid playerId, [FromServices] ICommandBus commandBus, [FromServices] IMyLogger logger,
        CancellationToken ct) =>
    {
        try
        {
            var command = new App.Application.UseCase.Matchmaking.LeaveMatchmaking.Command(matchmakingId, playerId);
            await commandBus
                .SendAsync(command, ct);
            return Results.NoContent();
        }
        catch (Exception e)
        {
            logger.Error(
                $"Error during leaving a matchmaking: {e.Message} (matchmakingId: {matchmakingId}, playerId: {playerId
                }");
            return Results.Problem("Could not leave");
        }
    });

app.MapGet("/matchmaking",
    async (Guid matchmakingId, [FromServices] ICommandBus commandBus, [FromServices] IMyLogger logger,
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
        catch (Exception e)
        {
            logger.Error($"Error during getting matchmaking info: {e.Message} (matchmakingId: {matchmakingId
            }), StackTrace: {e.StackTrace}");

            return Results.Problem("Could not join");
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

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.UseRouting();

if (mode == Mode.Offline)
{
    await OfflineTests.InitializeOfflineTest(app.Services,
        app.Services.GetRequiredService<App.Application.Utility.IMyLogger>());
}

app.Run();