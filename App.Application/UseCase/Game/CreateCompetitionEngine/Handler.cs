using App.Application.Abstractions;
using App.Application.UseCase.Competition.Engine.Creation;
using App.Application.Exception;
using App.Application.Ext;
using App.Domain.Competition;
using App.Domain.Repositories;
using App.Domain.Repository;

namespace App.Application.UseCase.Game.CreateCompetitionEngine;

public abstract record GameCompetitionType
{
    public sealed record PostDraftCompetition() : GameCompetitionType;

    public sealed record PreDraft(int Index) : GameCompetitionType;
}

public record Command(Domain.Game.Id.Id GameId, GameCompetitionType GameCompetitionType);

public class Handler(
    IGameRepository games,
    ICompetitionEngineSnapshotRepository competitionEngineSnapshots,
    ICompetitionEnginePluginRepository competitionEnginePlugins,
    Func<Guid, Hill> gameHillIdToCompetitionHill,
    Guid guid)
    : IApplicationHandler<Domain.Competition.Engine.Id, Command>
{
    public async Task<Domain.Competition.Engine.Id> HandleAsync(Command command, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var game = await FSharpAsyncExt.AwaitOrThrow(games.LoadAsync(command.GameId, ct),
            new IdNotFoundException(command.GameId.Item), ct);

        var engineRawOptions = game.Settings.CompetitionSettings.EngineRawOptions.ToDictionary();

        var competitionEnginePluginId = command.GameCompetitionType switch
        {
            GameCompetitionType.PreDraft preDraft => game.Settings.PreDraftSettings.Competitions[preDraft.Index]
                .CompetitionEnginePluginId,
            GameCompetitionType.PostDraftCompetition _ => game.Settings.CompetitionSettings
                .CompetitionEnginePluginId,
            _ => throw new ArgumentOutOfRangeException(),
        };

        var plugin = await competitionEnginePlugins.GetByIdAsync(competitionEnginePluginId, ct);
        var factory = plugin.Factory;

        var gameHillId = game.Settings.HillId;
        var competitionHill = gameHillIdToCompetitionHill(gameHillId.Item);

        var creationContext = new Context(guid, engineRawOptions, competitionHill);

        var engine = factory.Create(creationContext);

        await FSharpAsyncExt.AwaitOrThrow(competitionEngineSnapshots.SaveSnapshotById(engine.Id, engine.ToSnapshot()),
            new IdNotFoundException(command.GameId.Item), ct);

        return engine.Id;
    }
}