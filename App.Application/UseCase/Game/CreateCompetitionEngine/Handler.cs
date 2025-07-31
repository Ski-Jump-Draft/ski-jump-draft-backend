// using App.Application.Commanding;
// using App.Application.Exception;
// using App.Application.Ext;
// using App.Application.Factory;
// using App.Domain.Repositories;
// using App.Domain.Shared;
//
// namespace App.Application.UseCase.Game.CreateCompetitionEngine;
//
//
// public record Command(
//     Domain.Game.Id.Id GameId,
//     Domain.GameWorld.HillTypes.Id GameWorldHillId,
//     GameCompetitionType GameCompetitionType,
//     
//     )
//     : ICommand<Domain.Competition.Engine.Id>;
//
// /// <summary>
// /// Creates a `IEngine' for the competition and saves a snapshot into the repository. Creates a `Competition.Hill` and saves it into the repository.
// /// </summary>
// /// <param name="games"></param>
// /// <param name="competitionHills"></param>
// /// <param name="competitionEngineSnapshots"></param>
// /// <param name="competitionEnginePlugins"></param>
// /// <param name="competitionHillFactory"></param>
// /// <param name="guid"></param>
// public class Handler(
//     IGameRepository games,
//     IGameWorldHillRepository gameWorldHills,
//     ICompetitionEngineSnapshotRepository competitionEngineSnapshots,
//     ICompetitionEnginePluginRepository competitionEnginePlugins,
//     ICompetitionHillFactory competitionHillFactory,
//     IGuid guid)
//     : ICommandHandler<Command, Domain.Competition.Engine.Id>
// {
//     public async Task<Domain.Competition.Engine.Id> HandleAsync(Command command, MessageContext messageContext, CancellationToken ct)
//     {
//         ct.ThrowIfCancellationRequested();
//         var game = await games.LoadAsync(command.GameId, ct)
//             .AwaitOrWrap(_ => new IdNotFoundException<Guid>(command.GameId.Item));
//         var settings = game.Settings_;
//
//         var engineRawOptions = settings.CompetitionSettings.EngineRawOptions.ToDictionary();
//
//         var competitionEnginePluginId = command.GameCompetitionType switch
//         {
//             GameCompetitionType.PreDraft preDraft => settings.PreDraftSettings.Competitions[preDraft.Index]
//                 .CompetitionEnginePluginId,
//             GameCompetitionType.PostDraftCompetition _ => settings.CompetitionSettings
//                 .CompetitionEnginePluginId,
//             _ => throw new ArgumentOutOfRangeException(),
//         };
//
//         var plugin = await competitionEnginePlugins.GetByIdAsync(competitionEnginePluginId, ct) ??
//                      throw new IdNotFoundException<string>(competitionEnginePluginId);
//         var factory = plugin.Factory;
//
//         var gameWorldHill = await gameWorldHills.GetByIdAsync(command.GameWorldHillId)
//             .AwaitOrWrap(_ => new IdNotFoundException<Guid>(command.GameWorldHillId.Item));
//
//         var competitionHill = await competitionHillFactory.CreateAsync(gameWorldHill, ct);
//
//         await competitionHills.SaveAsync(competitionHill.Id, competitionHill);
//
//         var engineId = guid.NewGuid();
//
//         var creationContext = new Context(engineId, engineRawOptions, competitionHill);
//
//         var engine = factory.Create(creationContext);
//
//         await competitionEngineSnapshots.SaveAsync(engine.Id, engine.ToSnapshot());
//
//         return engine.Id;
//     }
// }