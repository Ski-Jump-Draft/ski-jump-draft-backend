using App.Application.Exception;
using App.Application.Ext;
using App.Domain.Game;
using App.Domain.Repositories;
using App.Domain.Shared;

namespace App.Application.Factory.Impl.GameHillFactory;

public class DefaultGameHillFactory(
    IGuid guid,
    IGameHillMapping gameHillMapping,
    IGameHillRepository gameHills)
    : IGameHillFactory
{
    public async Task<Domain.Game.Hill.Hill> CreateAsync(Domain.GameWorld.Hill gameWorldHill, CancellationToken ct)
    {
        if (gameHillMapping.TryMap(gameWorldHill.Id, out var gameHillId))
        {
            return await FSharpAsyncExt.AwaitOrThrow(gameHills.GetByIdAsync(gameHillId, ct),
                new IdNotFoundException<Guid>(gameHillId.Item), ct);
        }

        var newGameHillId = Domain.Game.Hill.Id.NewId(guid.NewGuid());
        var newGameHill = new Domain.Game.Hill.Hill(newGameHillId);
        return newGameHill;
    }
}