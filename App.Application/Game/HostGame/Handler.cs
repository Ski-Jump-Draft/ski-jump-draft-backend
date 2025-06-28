using App.Application.Exception;
using App.Application.Ext;
using App.Application.Game.Exception;
using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;
using App.Domain.Shared;
using App.Domain.Repositories;
using App.Domain.Time;

namespace App.Application.Game.HostGame;

public record Command(Ids.HostId HostId, Ids.ServerId ServerId, App.Domain.Game.GameModule.Settings Settings);

public class Handler(IGuid guid, IHostRepository hosts, IServerRepository servers, IGameRepository games, IClock clock)
{
    public async Task<Guid> HandleAsync(Command command, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var host = await FSharpAsyncExt.AwaitOrThrow(hosts.GetById(command.HostId), ct,
            new IdNotFoundException(command.HostId.Item));
        var server = await FSharpAsyncExt.AwaitOrThrow(servers.GetById(command.ServerId), ct,
            new IdNotFoundException(command.ServerId.Item));

        var hostHasAccess = host.Permissions.AllowedServers.Any(s => s.Id.Equals(command.ServerId));
        if (!hostHasAccess)
            throw new HostNoServerAccessException(host, server);

        var serverIsAvailable =
            (await FSharpAsync.StartAsTask(servers.IsAvailable(command.ServerId), null, null)).Value;

        if (!serverIsAvailable)
        {
            throw new ServerUnavailableException(server);
        }

        var game = CreateGame(command);

        await FSharpAsyncExt.AwaitOrThrow(games.Add(game), ct, new AddingGameFailedException(game));

        return game.Id.Item;
    }

    private App.Domain.Game.Game CreateGame(Command command)
    {
        var game = App.Domain.Game.Game.Create(guid, command.HostId, command.Settings, clock);
        return game;
    }
}