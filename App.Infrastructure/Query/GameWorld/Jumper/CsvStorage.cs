using System.Globalization;
using App.Application.ReadModel.Projection;
using App.Domain.GameWorld;
using App.Infrastructure.DataSourceHelpers;
using CsvHelper;

namespace App.Infrastructure.Query.GameWorld.Jumper;

public class CsvStorage(string path) : IGameWorldJumperQuery
{
    public async Task<IEnumerable<GameWorldJumperDto>> GetByIds(
        IEnumerable<JumperTypes.Id> gameWorldJumperIds,
        CancellationToken ct = default)
    {
        var all = await GetAllAsync(ct);
        var idSet = gameWorldJumperIds.ToHashSet();
        return all.Where(gameWorldJumperDto => idSet.Contains(JumperTypes.Id.NewId(gameWorldJumperDto.Id)));
    }

    public async Task<IEnumerable<GameWorldJumperDto>> GetAllAsync(CancellationToken ct)
    {
        var gameWorldJumperDtos = await new GameWorldJumpersLoader(path).LoadAllAsync(ct);
        return gameWorldJumperDtos;
    }
}