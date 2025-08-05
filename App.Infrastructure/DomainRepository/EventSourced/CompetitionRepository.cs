// using App.Application.Abstractions;
// using App.Application.CompetitionEngine;
// using App.Application.Factory;
// using App.Domain.Competition;
// using App.Domain.Repositories;
// using App.Domain.Shared;
// using App.Domain.Time;
// using Microsoft.FSharp.Core;
// using static Microsoft.FSharp.Collections.ListModule;
//
// namespace App.Infrastructure.DomainRepository.EventSourced;
//
// internal static class CompetitionEvolver
// {
//     public static async Task<Competition> FoldAsync(
//         ICompetitionEngineFactoryProvider factoryProvider,
//         IGameWorldHillRepository hillRepo,
//         ICompetitionHillFactory hillFactory,
//         IEnumerable<Event.CompetitionEventPayload> events)
//     {
//         var eventsArray = events as Event.CompetitionEventPayload[] ?? events.ToArray();
//         var competitionCreatedEventPayload =
//             eventsArray.OfType<Event.CompetitionEventPayload.CompetitionCreatedV1>().FirstOrDefault()
//             ?? throw new InvalidOperationException("Missing CompetitionCreatedV1");
//
//         var engine =
//             await CreateEngineAsync(factoryProvider, hillRepo, hillFactory, competitionCreatedEventPayload.Item);
//
//         var initializeEngine = FSharpFunc<Event.CompetitionCreatedV1, Engine.IEngine>.FromConverter(_ => engine);
//         return Evolve.fold(initializeEngine, eventsArray);
//     }
//
//     private static async Task<Engine.IEngine> CreateEngineAsync(
//         ICompetitionEngineFactoryProvider competitionEngineFactoryProvider,
//         IGameWorldHillRepository gameWorldHills,
//         ICompetitionHillFactory competitionHillFactory,
//         Event.CompetitionCreatedV1 competitionCreatedV1Payload)
//     {
//         var engineConfig = competitionCreatedV1Payload.EngineConfig;
//         var factory = competitionEngineFactoryProvider.Provide(new EngineFactoryRequest(
//             engineConfig.EngineName, engineConfig.EngineVersion));
//
//         var gameWorldHill = (await gameWorldHills.GetByIdAsync(engineConfig.GameWorldHillId)).Value;
//         var competitionHill = await competitionHillFactory.CreateAsync(gameWorldHill, CancellationToken.None);
//
//         var engineRawConfig = engineConfig.EngineRawConfig.ToDictionary();
//         var engineCreationContext = new CreationContext(engineRawConfig, competitionHill, engineConfig.RandomSeed);
//
//         return factory.Create(engineCreationContext);
//     }
// }
//
// public sealed class CompetitionRepository(
//     IEventStore<App.Domain.Competition.Id.Id, Event.CompetitionEventPayload> store,
//     ICompetitionEngineFactoryProvider factoryProvider,
//     IGameWorldHillRepository hillRepo,
//     ICompetitionHillFactory hillFactory,
//     IClock clock,
//     IGuid guid,
//     IEventBus eventBus)
//     :
//         DefaultEventSourcedRepository<
//             Competition,
//             App.Domain.Competition.Id.Id,
//             Event.CompetitionEventPayload>(store,
//             clock,
//             guid,
//             eventBus,
//             events => CompetitionEvolver.FoldAsync(factoryProvider, hillRepo, hillFactory,
//                 OfSeq(events.Select(@event => @event.Payload))),
//             p => App.Domain.Competition.Event.Versioning.schemaVersion(p)),
//         ICompetitionRepository;