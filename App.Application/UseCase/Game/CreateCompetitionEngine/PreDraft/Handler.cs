using App.Application.Commanding;
using App.Application.Exception;
using App.Application.Ext;
using App.Application.CompetitionEngine;
using App.Domain.Competition;
using App.Domain.Repositories;
using App.Domain.Shared;

namespace App.Application.UseCase.Game.CreateCompetitionEngine.PreDraft;

public record Command(
    Domain.Game.Id.Id GameId,
    Domain.GameWorld.HillTypes.Id GameWorldHillId,
    Domain.PreDraft.Settings.Settings PreDraftSettings,
    uint PreDraftCompetitionIndex)
    : ICommand<Domain.Competition.Engine.Id>;

public class Handler(
    IGuid guid,
    IGameRepository games,
    ICompetitionEnginePluginRepository competitionEnginePlugins,
    IGameWorldHillRepository gameWorldHills,
    ICompetitionHillFactory competitionHillFactory,
    ICompetitionEngineSnapshotRepository competitionEngineSnapshots)
    : ICommandHandler<Command, Domain.Competition.Engine.Id>
{
    public async Task<Engine.Id> HandleAsync(Command command, MessageContext messageContext, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var game = await games.LoadAsync(command.GameId, ct)
            .AwaitOrWrap(_ => new IdNotFoundException<Guid>(command.GameId.Item));
        var preDraftSettings = command.PreDraftSettings;
        var preDraftCompetitionSettings = preDraftSettings.CompetitionSettings[(int)command.PreDraftCompetitionIndex];

        var engineRawOptions = preDraftCompetitionSettings.EngineRawOptions.ToDictionary();
        var competitionEnginePluginId = preDraftCompetitionSettings.CompetitionEnginePluginId;

        var plugin = await competitionEnginePlugins.GetByIdAsync(competitionEnginePluginId, ct) ??
                     throw new IdNotFoundException<string>(competitionEnginePluginId);

        var gameWorldHill = await gameWorldHills.GetByIdAsync(command.GameWorldHillId)
            .AwaitOrWrap(_ => new IdNotFoundException<Guid>(command.GameWorldHillId.Item));

        var competitionHill = await competitionHillFactory.CreateAsync(gameWorldHill, ct);

        var engineId = guid.NewGuid();
        var engineCreationContext = new Context(engineId, engineRawOptions, competitionHill);
        var engine = plugin.Factory.Create(engineCreationContext);

        await competitionEngineSnapshots.SaveAsync(engine.Id, engine.ToSnapshot());

        return engine.Id;
    }
}