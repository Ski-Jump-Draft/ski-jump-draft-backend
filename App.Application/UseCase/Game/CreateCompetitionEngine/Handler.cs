using App.Application.Abstractions;
using App.Application.Abstractions.Mappers;
using App.Application.Exception;
using App.Application.Ext;
using App.Application.Factory;
using App.Domain.Competition;
using App.Domain.Repositories;
using App.Domain.Shared;

namespace App.Application.UseCase.Game.CreateCompetitionEngine;

public abstract record GameCompetitionType
{
    public sealed record PostDraftCompetition() : GameCompetitionType;

    public sealed record PreDraft(int Index) : GameCompetitionType;
}

public record Command(Domain.Game.Id.Id GameId, GameCompetitionType GameCompetitionType)
    : ICommand<Domain.Competition.Engine.Id>;

public class Handler(
    IGameRepository games,
    IGameHillRepository gameHills,
    ICompetitionEngineSnapshotRepository competitionEngineSnapshots,
    ICompetitionEnginePluginRepository competitionEnginePlugins,
    ICompetitionHillFactory competitionHillFactory,
    IGuid guid)
    : ICommandHandler<Command, Domain.Competition.Engine.Id>
{
    public async Task<Domain.Competition.Engine.Id> HandleAsync(Command command, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var game = await FSharpAsyncExt.AwaitOrThrow(games.LoadAsync(command.GameId, ct),
            new IdNotFoundException<Guid>(command.GameId.Item), ct);
        var settings = game.Settings_;

        var engineRawOptions = settings.CompetitionSettings.EngineRawOptions.ToDictionary();

        var competitionEnginePluginId = command.GameCompetitionType switch
        {
            GameCompetitionType.PreDraft preDraft => settings.PreDraftSettings.Competitions[preDraft.Index]
                .CompetitionEnginePluginId,
            GameCompetitionType.PostDraftCompetition _ => settings.CompetitionSettings
                .CompetitionEnginePluginId,
            _ => throw new ArgumentOutOfRangeException(),
        };

        var plugin = await competitionEnginePlugins.GetByIdAsync(competitionEnginePluginId, ct) ??
                     throw new IdNotFoundException<string>(competitionEnginePluginId);
        var factory = plugin.Factory;

        var gameHillId = settings.HillId;
        var gameHill = await FSharpAsyncExt.AwaitOrThrow(gameHills.GetByIdAsync(gameHillId, ct),
            new IdNotFoundException<Guid>(gameHillId.Item), ct);

        var competitionHill = await competitionHillFactory.CreateAsync(gameHill, ct);

        var engineId = guid.NewGuid();

        var creationContext = new Context(engineId, engineRawOptions, competitionHill);

        var engine = factory.Create(creationContext);

        await FSharpAsyncExt.AwaitOrThrow(competitionEngineSnapshots.SaveByIdAsync(engine.Id, engine.ToSnapshot()),
            new IdNotFoundException<Guid>(command.GameId.Item), ct);

        return engine.Id;
    }
}