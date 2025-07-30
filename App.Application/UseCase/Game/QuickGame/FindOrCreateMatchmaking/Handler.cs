using App.Application.UseCase.Helper;

namespace App.Application.UseCase.Game.QuickGame.FindOrCreateMatchmaking;

using Abstractions;
using Ext;
using Projection;
using Exception;
using App.Domain.Matchmaking;
using Domain.Shared;
using Domain.Repositories;

// TODO: Walidacja i nie raw string!
public record Command(
    string Nick
) : ICommand<Guid>;

public class Handler(
    IMatchmakingRepository matchmakings,
    IActiveGamesProjection activeGamesProjection,
    IActiveMatchmakingsProjection activeMatchmakingsProjection,
    IQuickGameMatchmakingSettingsProvider matchmakingSettingsProvider,
    IGuid guid
) : ICommandHandler<Command, Guid>
{
    public async Task<Guid> HandleAsync(Command command, CancellationToken ct)
    {
        var activeGames = (await activeGamesProjection.GetActiveGamesAsync(ct)).ToArray();
        if (activeGames.Length > 0)
        {
            throw new JoiningQuickMatchmakingFailedException(command.Nick,
                JoiningQuickMatchmakingFailReason.GameAlreadyRunning);
        }

        var activeMatchmakings = (await activeMatchmakingsProjection.GetActiveMatchmakingsAsync(ct)).ToArray();
        switch (activeMatchmakings.Length)
        {
            case 1:
            {
                var activeMatchmaking = activeMatchmakings.Single();
                var matchmakingId = App.Domain.Matchmaking.Id.NewId(activeMatchmaking.MatchmakingId);
                return matchmakingId.Item;
            }
            case > 1:
                // TODO: Dynamicznie wybieramy serwer - wg dostępności, regionu
                throw new NotImplementedException("Multiple active matchmakings not supported yet.");
            default:
            {
                var matchmakingId = Id.NewId(guid.NewGuid());
                var aggregateVersion = AggregateVersion.AggregateVersion.NewAggregateVersion(0u);
                // TODO: Może hosty do matchmakingów?
                // 
                // var hostId = App.Domain.Matchmaking.Hosting.HostModule.Id.NewId(getGlobalHostId());
                var settings = await matchmakingSettingsProvider.Provide();
                var matchmaking =
                    Matchmaking.Create(matchmakingId, aggregateVersion, settings);
                if (!matchmaking.IsOk)
                    throw new JoiningQuickMatchmakingFailedException(command.Nick,
                        JoiningQuickMatchmakingFailReason.ErrorDuringSettingUp);

                var (matchmakingAggregate, events) = matchmaking.ResultValue;

                var correlationId = guid.NewGuid();
                var causationId = correlationId;
                var expectedVersion = matchmakingAggregate.Version_;
                await
                    matchmakings.SaveAsync(matchmakingAggregate, events, expectedVersion, correlationId, causationId,
                            ct)
                        .AwaitOrWrap(_ => new JoiningQuickMatchmakingFailedException(command.Nick,
                            JoiningQuickMatchmakingFailReason.ErrorDuringSettingUp));
                return matchmakingId.Item;
            }
        }
    }
}