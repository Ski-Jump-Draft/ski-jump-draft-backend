using App.Application.Exception;
using App.Application.Ext;
using App.Application.Game.Exception;
using App.Domain.Game;
using App.Domain.Game.Hosting;
using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;
using App.Domain.Shared;
using App.Domain.Repositories;
using App.Domain.Repository;
using App.Domain.Time;

namespace App.Application.Game.HostGame;

public record Command(HostModule.Id HostId, ServerModule.Id ServerId, App.Domain.Game.Settings.Settings Settings);

public class Handler(IGuid guid, IHostRepository hosts, IServerRepository servers, IGameRepository games, IClock clock)
{
    public async Task<App.Domain.Game.Id.Id> HandleAsync(Command command, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var host = await FSharpAsyncExt.AwaitOrThrow(hosts.GetById(command.HostId),
            new IdNotFoundException(command.HostId.Item), ct);
        var server = await FSharpAsyncExt.AwaitOrThrow(servers.GetById(command.ServerId),
            new IdNotFoundException(command.ServerId.Item), ct);

        var hostHasAccess = host.Permissions.AllowedServers.Any(s => s.Id.Equals(command.ServerId));
        if (!hostHasAccess)
            throw new HostNoServerAccessException(host, server);

        var serverIsAvailable =
            (await FSharpAsync.StartAsTask(servers.IsAvailable(command.ServerId), null, null)).Value;

        if (!serverIsAvailable)
        {
            throw new ServerUnavailableException(server);
        }

        var gameId = Domain.Game.Id.Id.NewId(guid.NewGuid());
        var gameVersion = AggregateVersion.AggregateVersion.NewAggregateVersion(0u);

        var gameCreationResult = App.Domain.Game.Game.Create(gameId,
            gameVersion,
            command.HostId, command.Settings);

        if (!gameCreationResult.IsOk) throw new NotImplementedException();

        var (game, events) = gameCreationResult.ResultValue;

        var correlationId = guid.NewGuid();
        var causationId = correlationId;
        var expectedVersion = game.Version;

        await FSharpAsyncExt.AwaitOrThrow(
            games.SaveAsync(game, events, expectedVersion, correlationId, causationId),
            new CreatingGameFailedUnknownException(host, command.Settings),
            ct
        );

        return game.Id;
    }
}