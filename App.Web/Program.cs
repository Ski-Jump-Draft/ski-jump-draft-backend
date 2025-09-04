using System.Globalization;
using App.Application.Acl;
using App.Application.Commanding;
using App.Application.Matchmaking;
using App.Application.Messaging.Notifiers;
using App.Application.Policy.GameHillSelector;
using App.Application.Policy.GameJumpersSelector;
using App.Application.Utility;
using App.Domain.GameWorld;
using App.Infrastructure.Helper.Csv;
using App.Web.MockedFlow;
using App.Web.Notifiers.Game;
using App.Web.Notifiers.SseHub;
using App.Web.SignalR.Hub;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
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

builder.Services.AddSingleton<IGuid, App.Infrastructure.Utility.Guid.SystemGuid>();
builder.Services.AddSingleton<IClock, App.Infrastructure.Utility.Clock.SystemClock>();
builder.Services.AddSingleton<IRandom, App.Infrastructure.Utility.Random.SystemRandom>();
builder.Services.AddSingleton<IJson, App.Infrastructure.Utility.Json.DefaultJson>();
builder.Services.AddSingleton<ICommandBus, App.Infrastructure.Commanding.CommandBus.InMemory>();
builder.Services.AddSingleton<IScheduler, App.Infrastructure.Commanding.Scheduler.InMemory>();

builder.Services
    .AddSingleton<
        ICommandHandler<App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.Command,
            App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.Result>,
        App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.Handler>();
builder.Services
    .AddSingleton<
        ICommandHandler<App.Application.UseCase.Matchmaking.EndMatchmaking.Command,
            App.Application.UseCase.Matchmaking.EndMatchmaking.Result>,
        App.Application.UseCase.Matchmaking.EndMatchmaking.Handler>();
builder.Services
    .AddSingleton<
        ICommandHandler<App.Application.UseCase.Matchmaking.LeaveMatchmaking.Command>,
        App.Application.UseCase.Matchmaking.LeaveMatchmaking.Handler>();
builder.Services
    .AddSingleton<
        ICommandHandler<App.Application.UseCase.Matchmaking.GetMatchmaking.Command,
            App.Application.UseCase.Matchmaking.GetMatchmaking.Result>,
        App.Application.UseCase.Matchmaking.GetMatchmaking.Handler>();
builder.Services
    .AddSingleton<
        ICommandHandler<App.Application.UseCase.Game.StartGame.Command,
            App.Application.UseCase.Game.StartGame.Result>,
        App.Application.UseCase.Game.StartGame.Handler>();
builder.Services
    .AddSingleton<
        ICommandHandler<App.Application.UseCase.Game.StartPreDraft.Command,
            App.Application.UseCase.Game.StartPreDraft.Result>,
        App.Application.UseCase.Game.StartPreDraft.Handler>();
builder.Services
    .AddSingleton<
        ICommandHandler<App.Application.UseCase.Game.StartNextPreDraftCompetition.Command,
            App.Application.UseCase.Game.StartNextPreDraftCompetition.Result>,
        App.Application.UseCase.Game.StartNextPreDraftCompetition.Handler>();
builder.Services
    .AddSingleton<
        ICommandHandler<App.Application.UseCase.Game.StartDraft.Command,
            App.Application.UseCase.Game.StartDraft.Result>,
        App.Application.UseCase.Game.StartDraft.Handler>();
builder.Services
    .AddSingleton<
        ICommandHandler<App.Application.UseCase.Game.PickJumper.Command,
            App.Application.UseCase.Game.PickJumper.Result>,
        App.Application.UseCase.Game.PickJumper.Handler>();
builder.Services
    .AddSingleton<
        ICommandHandler<App.Application.UseCase.Game.PassPick.Command,
            App.Application.UseCase.Game.PassPick.Result>,
        App.Application.UseCase.Game.PassPick.Handler>();
builder.Services
    .AddSingleton<
        ICommandHandler<App.Application.UseCase.Game.SimulateJump.Command,
            App.Application.UseCase.Game.SimulateJump.Result>,
        App.Application.UseCase.Game.SimulateJump.Handler>();
builder.Services
    .AddSingleton<
        ICommandHandler<App.Application.UseCase.Game.StartMainCompetition.Command,
            App.Application.UseCase.Game.StartMainCompetition.Result>,
        App.Application.UseCase.Game.StartMainCompetition.Handler>();
builder.Services
    .AddSingleton<
        ICommandHandler<App.Application.UseCase.Game.LeaveGame.Command,
            App.Application.UseCase.Game.LeaveGame.Result>,
        App.Application.UseCase.Game.LeaveGame.Handler>();
builder.Services
    .AddSingleton<
        ICommandHandler<App.Application.UseCase.Game.EndGame.Command,
            App.Application.UseCase.Game.EndGame.Result>,
        App.Application.UseCase.Game.EndGame.Handler>();


builder.Services.AddSingleton<App.Domain.Matchmaking.Settings>(sp =>
    App.Domain.Matchmaking.Settings.Create(App.Domain.Matchmaking.SettingsModule.MinPlayersModule.create(3).Value,
        App.Domain.Matchmaking.SettingsModule.MaxPlayersModule.create(5).Value).ResultValue);

builder.Services.AddSingleton<App.Domain.Game.Settings>(sp =>
{
    var preDraftCompetitionSettings = App.Domain.Competition.Settings.Create(ListModule.OfSeq(new[]
        {
            new App.Domain.Competition.RoundSettings(App.Domain.Competition.RoundLimit.NoneLimit, false, false)
        }))
        .ResultValue;
    var preDraftSettings = App.Domain.Game.PreDraftSettings.Create(ListModule.OfSeq(
            new List<App.Domain.Competition.Settings>
                { preDraftCompetitionSettings /*, preDraftCompetitionSettings */ }))
        .Value;
    var mainCompetitionSettings = App.Domain.Competition.Settings.Create(ListModule.OfSeq(new[]
    {
        new App.Domain.Competition.RoundSettings(
            App.Domain.Competition.RoundLimit.NewSoft(App.Domain.Competition.RoundLimitValueModule.tryCreate(50)
                .ResultValue), false, false),
        new App.Domain.Competition.RoundSettings(
            App.Domain.Competition.RoundLimit.NewSoft(App.Domain.Competition.RoundLimitValueModule.tryCreate(30)
                .ResultValue), true, false)
    })).ResultValue;
    var draftSettings = new App.Domain.Game.DraftModule.Settings(
        App.Domain.Game.DraftModule.SettingsModule.TargetPicksModule.create(4).Value,
        App.Domain.Game.DraftModule.SettingsModule.MaxPicksModule.create(4).Value,
        App.Domain.Game.DraftModule.SettingsModule.UniqueJumpersPolicy.Unique,
        App.Domain.Game.DraftModule.SettingsModule.Order.Snake,
        App.Domain.Game.DraftModule.SettingsModule.TimeoutPolicy.NewTimeoutAfter(TimeSpan.FromSeconds(15)));
    return new App.Domain.Game.Settings(preDraftSettings, draftSettings, mainCompetitionSettings,
        App.Domain.Game.RankingPolicy.Classic);
});


builder.Services.AddSingleton<App.Application.Utility.IMyLogger, App.Infrastructure.Utility.Logger.Dotnet>();

builder.Services
    .AddSingleton<App.Domain.Matchmaking.IMatchmakings, App.Infrastructure.Repository.Matchmaking.InMemory>();
builder.Services.AddSingleton<IMatchmakingSchedule, App.Infrastructure.Matchmaking.Schedule.InMemory>();

builder.Services.AddSingleton<App.Domain.Game.IGames, App.Infrastructure.Repository.Game.InMemory>();

builder.Services.AddHostedService<BotJoiner>();

// SSE, WS
builder.Services.AddSingleton<App.Web.Notifiers.SseHub.ISseHub, App.Web.Notifiers.SseHub.Default>();
builder.Services.AddSingleton<IMatchmakingNotifier, App.Web.Notifiers.Matchmaking.Sse>();
builder.Services.AddSingleton<IGameNotifier, SignalRGameNotifier>();

builder.Services.AddSingleton<IGameHillSelector, App.Application.Policy.GameHillSelector.Fixed>(sp =>
    new Fixed("Zakopane HS140", sp.GetRequiredService<IHills>()));
builder.Services.AddSingleton<IGameJumpersSelector, App.Application.Policy.GameJumpersSelector.All>();

builder.Services.AddSingleton<IGameJumperAcl, App.Infrastructure.Acl.GameJumpers.InMemory>();
builder.Services.AddSingleton<ICompetitionJumperAcl, App.Infrastructure.Acl.CompetitionJumper.InMemory>();
builder.Services.AddSingleton<ICompetitionHillAcl, App.Infrastructure.Acl.CompetitionHill.InMemory>();

builder.Services
    .AddSingleton<App.Application.Draft.IDraftPassPicker, App.Application.Draft.PassPicker.RandomPicker>();
builder.Services
    .AddSingleton<App.Application.Game.Ranking.IGameRankingFactorySelector,
        App.Application.Game.Ranking.DefaultSelector>();

builder.Services.AddSingleton<App.Domain.Simulation.IWeatherEngine, App.Simulator.Mock.WeatherEngine>();
builder.Services.AddSingleton<App.Domain.Simulation.IJumpSimulator, App.Simulator.Simple.JumpSimulator>();

builder.Services
    .AddSingleton<App.Application.Matchmaking.IMatchmakingDurationCalculator,
        App.Application.Matchmaking.FixedMatchmakingDurationCalculator>(sp =>
        new FixedMatchmakingDurationCalculator(TimeSpan.FromSeconds(25)));
builder.Services
    .AddSingleton<App.Application.Game.DraftPicks.IDraftPicksArchive, App.Infrastructure.Archive.DraftPicks.InMemory>();

builder.Services.AddMemoryCache();

builder.Services
    .AddSingleton<App.Domain.GameWorld.ICountries, App.Infrastructure.Repository.GameWorld.Country.Csv>();
builder.Services
    .AddSingleton<App.Infrastructure.Repository.GameWorld.Country.IGameWorldCountriesCsvStreamProvider>(sp =>
    {
        var relPath = sp.GetRequiredService<IConfiguration>().GetValue<string>("Csv:Countries:File");
        var absPath = Path.Combine(AppContext.BaseDirectory, relPath!);

        var inner = new App.Infrastructure.Helper.Csv.FileCsvStreamProvider(absPath);
        return new CachingCsvStreamProvider(inner,
            sp.GetRequiredService<IMemoryCache>(),
            "countries",
            TimeSpan.FromMinutes(5));
    });

builder.Services
    .AddSingleton<App.Domain.GameWorld.IJumpers, App.Infrastructure.Repository.GameWorld.Jumper.Csv>();

builder.Services
    .AddSingleton<App.Infrastructure.Repository.GameWorld.Jumper.IGameWorldJumpersCsvStreamProvider>(sp =>
    {
        var relPath = sp.GetRequiredService<IConfiguration>().GetValue<string>("Csv:Jumpers:File");
        var absPath = Path.Combine(AppContext.BaseDirectory, relPath!);

        var inner = new App.Infrastructure.Helper.Csv.FileCsvStreamProvider(absPath);
        return new CachingCsvStreamProvider(inner,
            sp.GetRequiredService<IMemoryCache>(),
            "jumpers",
            TimeSpan.FromMinutes(5));
    });
builder.Services
    .AddSingleton<App.Domain.GameWorld.IHills, App.Infrastructure.Repository.GameWorld.Hill.Csv>();
builder.Services
    .AddSingleton<App.Infrastructure.Repository.GameWorld.Hill.IGameWorldHillsCsvStreamProvider>(sp =>
    {
        var relPath = sp.GetRequiredService<IConfiguration>().GetValue<string>("Csv:Hills:File");
        var absPath = Path.Combine(AppContext.BaseDirectory, relPath!);

        var inner = new App.Infrastructure.Helper.Csv.FileCsvStreamProvider(absPath);
        return new CachingCsvStreamProvider(inner,
            sp.GetRequiredService<IMemoryCache>(),
            "hills",
            TimeSpan.FromMinutes(5));
    });
builder.Services
    .AddSingleton<App.Infrastructure.Helper.Csv.GameWorldCountryIdProvider.IGameWorldCountryIdProvider,
        App.Infrastructure.Helper.Csv.GameWorldCountryIdProvider.Impl.Repository>();

builder.Services.AddSingleton(new CsvConfiguration(CultureInfo.InvariantCulture)
{
    HasHeaderRecord = true,
    MissingFieldFound = null,
    Delimiter = ","
});


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

app.MapHub<GameHub>("/game/hub");

app.UseRouting();

app.Run();