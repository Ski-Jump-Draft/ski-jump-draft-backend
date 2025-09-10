using System.Globalization;
using App.Application.Acl;
using App.Application.Commanding;
using App.Application.Exceptions;
using App.Application.Extensions;
using App.Application.Game.Gate;
using App.Application.JumpersForm;
using App.Application.Matchmaking;
using App.Application.Messaging.Notifiers;
using App.Application.Messaging.Notifiers.Mapper;
using App.Application.Policy.GameGateSelector;
using App.Application.Policy.GameHillSelector;
using App.Application.Policy.GameJumpersSelector;
using App.Application.Utility;
using App.Domain.GameWorld;
using App.Domain.Simulation;
using App.Simulator.Simple;
using CsvHelper.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FSharp.Collections;
using Playground.Game.Bot.Service;
using Playground.Game.Notifier;

namespace Playground.Game;

public static class DependencyInjection
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IGuid, App.Infrastructure.Utility.Guid.SystemGuid>();
        services.AddSingleton<IClock, App.Infrastructure.Utility.Clock.SystemClock>();
        services.AddSingleton<IRandom, App.Infrastructure.Utility.Random.SystemRandom>();
        services.AddSingleton<IJson, App.Infrastructure.Utility.Json.DefaultJson>();
        services.AddSingleton<ICommandBus, App.Infrastructure.Commanding.CommandBus.InMemory>();
        services.AddSingleton<IScheduler, App.Infrastructure.Commanding.Scheduler.InMemory>();
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
        services.AddSingleton<App.Domain.Matchmaking.Settings>(sp =>
            App.Domain.Matchmaking.Settings.Create(
                App.Domain.Matchmaking.SettingsModule.MinPlayersModule.create(5).Value,
                App.Domain.Matchmaking.SettingsModule.MaxPlayersModule.create(8).Value).ResultValue);

        services.AddSingleton<App.Domain.Game.Settings>(sp =>
        {
            var preDraftCompetitionSettings = App.Domain.Competition.Settings.Create(ListModule.OfSeq(new[]
                {
                    new App.Domain.Competition.RoundSettings(App.Domain.Competition.RoundLimit.NoneLimit, false,
                        false)
                }))
                .ResultValue;
            var preDraftSettings = App.Domain.Game.PreDraftSettings.Create(ListModule.OfSeq(
                    new List<App.Domain.Competition.Settings>
                        { preDraftCompetitionSettings }))
                .Value;
            var mainCompetitionSettings = App.Domain.Competition.Settings.Create(ListModule.OfSeq(new[]
            {
                new App.Domain.Competition.RoundSettings(
                    App.Domain.Competition.RoundLimit.NewSoft(App.Domain.Competition.RoundLimitValueModule
                        .tryCreate(50)
                        .ResultValue), false, false),
                new App.Domain.Competition.RoundSettings(
                    App.Domain.Competition.RoundLimit.NewSoft(App.Domain.Competition.RoundLimitValueModule
                        .tryCreate(20)
                        .ResultValue), true, true)
            })).ResultValue;
            var draftSettings = new App.Domain.Game.DraftModule.Settings(
                App.Domain.Game.DraftModule.SettingsModule.TargetPicksModule.create(2).Value,
                App.Domain.Game.DraftModule.SettingsModule.MaxPicksModule.create(2).Value,
                App.Domain.Game.DraftModule.SettingsModule.UniqueJumpersPolicy.Unique,
                App.Domain.Game.DraftModule.SettingsModule.Order.Snake,
                App.Domain.Game.DraftModule.SettingsModule.TimeoutPolicy.NewTimeoutAfter(
                    TimeSpan.FromSeconds(3)));
            return new App.Domain.Game.Settings(preDraftSettings, draftSettings, mainCompetitionSettings,
                App.Domain.Game.RankingPolicy.Classic);
        });


        services.AddSingleton<App.Application.Utility.IMyLogger, App.Infrastructure.Utility.Logger.Dotnet>();

        services
            .AddSingleton<App.Domain.Matchmaking.IMatchmakings,
                App.Infrastructure.Repository.Matchmaking.InMemory>();
        services.AddSingleton<IMatchmakingSchedule, App.Infrastructure.Matchmaking.Schedule.InMemory>();

        services.AddSingleton<App.Domain.Game.IGames, App.Infrastructure.Repository.Game.InMemory>();

        services.AddHostedService<Joiner>();

        services.AddSingleton<IMatchmakingNotifier, LambdaMatchmakingNotifier>(sp =>
        {
            var myPlayerData = sp.GetRequiredService<MyPlayerData>();
            return new LambdaMatchmakingNotifier(handleMatchmakingUpdate: matchmaking =>
            {
                if (matchmaking.Players.Any(dto => dto.Nick == myPlayerData.Nick))
                {
                    switch (matchmaking.Status)
                    {
                        case "Ended Succeeded":
                            Console.WriteLine($"Matchmaking zakończony");
                            break;
                        case "Ended NotEnoughPlayers":
                            Console.WriteLine($"Nie uzbierano wystarczającej ilości graczy w Matchmakingu");
                            break;
                    }
                }
                else
                {
                    Console.WriteLine($"Matchmaking {matchmaking} nie jest dla Ciebie");
                }
            }, handlePlayerJoin: playerJoin =>
            {
                if (playerJoin.Player.Nick == myPlayerData.Nick)
                {
                    Console.WriteLine($"Dołączyłeś do matchmakingu");
                }
                else
                {
                    var minRequiredPlayers = playerJoin.MinRequiredPlayers;
                    var playersCount = playerJoin.PlayersCount;
                    var maxPlayersCount = playerJoin.MaxPlayers;
                    if (minRequiredPlayers is null)
                    {
                        Console.WriteLine($"{playerJoin.Player.Nick
                        } dołączył do matchmakingu. Za chwilę możemy zaczynać ({playersCount}/{maxPlayersCount})");
                    }
                    else
                    {
                        Console.WriteLine($"{playerJoin.Player.Nick} dołączył do matchmakingu. Potrzebujemy jeszcze {
                            minRequiredPlayers} graczy");
                    }
                }
            }, handlePlayerLeft: playerLeave =>
            {
                if (playerLeave.Player.Nick == myPlayerData.Nick)
                {
                    Console.WriteLine($"Opuściłeś matchmaking");
                }
                else
                {
                    var minRequiredPlayers = playerLeave.MinRequiredPlayers;
                    var playersCount = playerLeave.PlayersCount;
                    var maxPlayersCount = playerLeave.MaxPlayers;
                    if (minRequiredPlayers is null)
                    {
                        Console.WriteLine($"{playerLeave.Player.Nick
                        } wyszedł z matchmakingu. Za chwilę możemy zaczynać ({playersCount}/{maxPlayersCount})");
                    }
                    else
                    {
                        Console.WriteLine($"{playerLeave.Player.Nick} wyszedł z matchmakingu. Potrzebujemy jeszcze {
                            minRequiredPlayers} graczy");
                    }
                }
            });
        });
        services.AddSingleton<IGameNotifier, ConsoleGameNotifier>();

        services.AddSingleton<IGameHillSelector, App.Application.Policy.GameHillSelector.Fixed>(sp =>
            new Fixed("Oslo HS134", sp.GetRequiredService<IHills>()));
        services.AddSingleton<IGameJumpersSelector, App.Application.Policy.GameJumpersSelector.All>();

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


        services
            .AddSingleton<App.Application.Draft.IDraftPassPicker, App.Application.Draft.PassPicker.RandomPicker>();
        services
            .AddSingleton<App.Application.Game.Ranking.IGameRankingFactorySelector,
                App.Application.Game.Ranking.DefaultSelector>();

        services
            .AddSingleton<GameUpdatedDtoMapper, GameUpdatedDtoMapper>();

        const double baseFormFactor = 3;
        services.AddSingleton<App.Simulator.Simple.SimulatorConfiguration>(sp =>
            new SimulatorConfiguration(SkillImpactFactor: 2, AverageBigSkill: 7,
                FlightToTakeoffRatio: 1, RandomAdditionsRatio: 1, TakeoffRatingPointsByForm: baseFormFactor * 0.9,
                FlightRatingPointsByForm: baseFormFactor * 1.1));
        services.AddSingleton<App.Domain.Simulation.IWeatherEngine, App.Simulator.Simple.WeatherEngine>(sp =>
            new WeatherEngine(sp.GetRequiredService<IRandom>(), sp.GetRequiredService<IMyLogger>(),
                ConfigurationPresetFactory.StableTailwind));
        services.AddSingleton<App.Domain.Simulation.IJumpSimulator, App.Simulator.Simple.JumpSimulator>();
        services.AddSingleton<App.Domain.Simulation.IJudgesSimulator, App.Simulator.Simple.JudgesSimulator>();

        services
            .AddSingleton<App.Application.Game.Gate.ISelectGameStartingGateService,
                App.Application.Game.Gate.SelectCompetitionStartingGateService>();

        services
            .AddSingleton<App.Application.Matchmaking.IMatchmakingDurationCalculator,
                App.Application.Matchmaking.FixedMatchmakingDurationCalculator>(sp =>
                new FixedMatchmakingDurationCalculator(TimeSpan.FromSeconds(20)));
        services
            .AddSingleton<App.Application.Game.DraftPicks.IDraftPicksArchive,
                App.Infrastructure.Archive.DraftPicks.InMemory>();
        services
            .AddSingleton<App.Application.Game.GameCompetitions.IGameCompetitionResultsArchive, App.Infrastructure.
                Archive.
                GameCompetitionResults.
                InMemory>();
        services
            .AddSingleton<App.Application.Game.Gate.IGameStartingGateSelectorFactory,
                App.Application.Game.Gate.IterativeSimulatedFactory>(sp =>
            {
                const JuryBravery juryBravery = JuryBravery.Medium;
                return new IterativeSimulatedFactory(sp.GetRequiredService<IJumpSimulator>(),
                    sp.GetRequiredService<IWeatherEngine>(),
                    juryBravery, sp.GetRequiredService<ICompetitionJumperAcl>(),
                    sp.GetRequiredService<IGameJumperAcl>(),
                    sp.GetRequiredService<ICompetitionHillAcl>(), sp.GetRequiredService<IJumpers>(),
                    sp.GetRequiredService<IJumperGameFormStorage>());
            });
        services
            .AddSingleton<App.Application.JumpersForm.IJumperGameFormAlgorithm,
                App.Application.Policy.GameFormAlgorithm.FullyRandom>();
        services
            .AddSingleton<App.Application.JumpersForm.IJumperGameFormStorage,
                App.Infrastructure.Storage.JumperGameForm.InMemory>();
        // services
        //     .AddSingleton<App.Application.JumpersForm.IJumperLiveFormProvider,
        //     >();

        services
            .AddSingleton<App.Domain.GameWorld.ICountries, App.Infrastructure.Repository.GameWorld.Country.Csv>();
        services
            .AddSingleton<App.Infrastructure.Repository.GameWorld.Country.IGameWorldCountriesCsvStreamProvider>(sp =>
            {
                var relPath = sp.GetRequiredService<IConfiguration>().GetValue<string>("Csv:Countries:File");
                var absPath = Path.Combine(AppContext.BaseDirectory, relPath!);
                return new App.Infrastructure.Helper.Csv.FileCsvStreamProvider(absPath);
            });

        services
            .AddSingleton<App.Domain.GameWorld.IJumpers, App.Infrastructure.Repository.GameWorld.Jumper.Csv>();

        services
            .AddSingleton<App.Infrastructure.Repository.GameWorld.Jumper.IGameWorldJumpersCsvStreamProvider>(sp =>
            {
                var relPath = sp.GetRequiredService<IConfiguration>().GetValue<string>("Csv:Jumpers:File");
                var absPath = Path.Combine(AppContext.BaseDirectory, relPath!);
                return new App.Infrastructure.Helper.Csv.FileCsvStreamProvider(absPath);
            });
        services
            .AddSingleton<App.Domain.GameWorld.IHills, App.Infrastructure.Repository.GameWorld.Hill.Csv>();
        services
            .AddSingleton<App.Infrastructure.Repository.GameWorld.Hill.IGameWorldHillsCsvStreamProvider>(sp =>
            {
                var relPath = sp.GetRequiredService<IConfiguration>().GetValue<string>("Csv:Hills:File");
                var absPath = Path.Combine(AppContext.BaseDirectory, relPath!);
                return new App.Infrastructure.Helper.Csv.FileCsvStreamProvider(absPath);
            });

        services.AddSingleton(new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            Delimiter = ","
        });

        // --- PLAYGROUND-SPECIFIC --- //
        services
            .AddSingleton<MyPlayerData>(sp => new MyPlayerData("SiekamCebulę"));
    }
}