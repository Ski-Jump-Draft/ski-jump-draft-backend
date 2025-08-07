using Dapper;
using System.Data;
using App.Application.Abstractions;
using App.Domain.Draft;

namespace App.Infrastructure.DraftToGameMapStore;

public class SqlDraftToGameMapStore : IDraftToGameMapStore
{
    private readonly IDbConnection _conn;

    public SqlDraftToGameMapStore(IDbConnection conn)
    {
        _conn = conn;
    }

    public async Task<MapResult> TryGetGameIdAsync(Id.Id draftId, CancellationToken ct)
    {
        var sql = "SELECT GameId FROM DraftToGameMap WHERE DraftId = @DraftId";
        var gameId =
            await _conn.QueryFirstOrDefaultAsync<System.Guid?>(new CommandDefinition(sql,
                new { DraftId = draftId.Item }, cancellationToken: ct));

        return new MapResult(gameId.HasValue, gameId.HasValue ? Domain.Game.Id.Id.NewId(gameId.Value) : null);
    }

    public Task AddMappingAsync(Id.Id draftId, Domain.Game.Id.Id gameId, CancellationToken ct)
    {
        var sql = "INSERT INTO DraftToGameMap (DraftId, GameId) VALUES (@DraftId, @GameId)";
        return _conn.ExecuteAsync(new CommandDefinition(sql, new { DraftId = draftId.Item, GameId = gameId.Item },
            cancellationToken: ct));
    }
}