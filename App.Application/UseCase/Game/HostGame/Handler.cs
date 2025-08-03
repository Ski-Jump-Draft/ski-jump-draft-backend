// using App.Application.Abstractions;
// using App.Application.Exception;
// using App.Application.Ext;
// using App.Application.UseCase.Game.Exception;
// using App.Domain.Game;
// using App.Domain.Game.Hosting;
// using Microsoft.FSharp.Control;
// using Microsoft.FSharp.Core;
// using App.Domain.Shared;
// using App.Domain.Repositories;
// using App.Domain.Repositories;
// using App.Domain.Time;
//
// namespace App.Application.UseCase.Game.HostGame;
//
// public record Command(HostModule.Id HostId, ServerModule.Id ServerId, App.Domain.Game.Settings.Settings Settings) : ICommand<App.Domain.Game.Id.Id>;
//
// public class Handler(IGuid guid, IHostRepository hosts, IServerRepository servers, IGameRepository games) : ICommandHandler<Command, App.Domain.Game.Id.Id>
// {
//     public async Task<App.Domain.Game.Id.Id> HandleAsync(Command command, CancellationToken ct)
//     {
//         ct.ThrowIfCancellationRequested();
//         var host = await FSharpAsyncExt.AwaitOrThrow(hosts.GetByIdAsync(command.HostId),
//             new IdNotFoundException<Guid>(command.HostId.Item), ct);
//         var server = await FSharpAsyncExt.AwaitOrThrow(servers.GetByIdAsync(command.ServerId),
//             new IdNotFoundException<Guid>(command.ServerId.Item), ct);
//
//         var hostHasAccess = host.Permissions.AllowedServers.Any(s => s.Id.Equals(command.ServerId));
//         if (!hostHasAccess)
//             throw new HostNoServerAccessException(host, server);
//
//         var serverIsAvailable =
//             (await FSharpAsync.StartAsTask(servers.IsAvailable(command.ServerId), null, null)).Value;
//
//         if (!serverIsAvailable)
//         {
//             throw new ServerUnavailableException(server);
//         }
//
//         var gameId = Domain.Game.Id.Id.NewId(guid.NewGuid());
//         var gameVersion = AggregateVersion.zero;
//
//         var gameCreationResult = App.Domain.Game.Game.Create(gameId,
//             gameVersion,
//             command.HostId, command.Settings);
//
//         if (!gameCreationResult.IsOk) throw new NotImplementedException();
//
//         var (game, events) = gameCreationResult.ResultValue;
//
//         var correlationId = guid.NewGuid();
//         var causationId = correlationId;
//         var expectedVersion = game.Version_;
//
//         await FSharpAsyncExt.AwaitOrThrow(
//             games.SaveAsync(game, events, expectedVersion, correlationId, causationId, ct),
//             new CreatingGameFailedUnknownException(host, command.Settings),
//             ct
//         );
//
//         return game.Id_;
//     }
// }