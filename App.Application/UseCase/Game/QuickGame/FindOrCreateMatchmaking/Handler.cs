using App.Application.ReadModel.Projection;
using App.Application.UseCase.Helper;

namespace App.Application.UseCase.Game.QuickGame.FindOrCreateMatchmaking;

using Abstractions;
using Ext;
using Exception;
using Domain.Shared;
using Domain.Repositories;

// TODO: Walidacja i nie raw string!
public record Command(
    string Nick
) : ICommand<Domain.Matchmaking.Id>;

public class Handler(
    IMatchmakingRepository matchmakings,
    IActiveGamesProjection activeGamesProjection,
    IActiveMatchmakingsProjection activeMatchmakingsProjection,
    IQuickGameMatchmakingSettingsProvider matchmakingSettingsProvider,
    IGuid guid
) : ICommandHandler<Command, Domain.Matchmaking.Id>
{
    public async Task<Domain.Matchmaking.Id> HandleAsync(Command command, MessageContext messageContext,
        CancellationToken ct)
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
                var matchmakingId = Domain.Matchmaking.Id.NewId(activeMatchmaking.MatchmakingId);
                return matchmakingId;
            }
            case > 1:
                // TODO: Dynamicznie wybieramy serwer - wg dostępności, regionu
                throw new NotImplementedException("Multiple active matchmakings aren't supported yet.");
            default:
            {
                var newMatchmakingId = Domain.Matchmaking.Id.NewId(guid.NewGuid());
                var aggregateVersion = AggregateVersion.zero;

                var settings = await matchmakingSettingsProvider.Provide();

                var matchmakingResult =
                    Domain.Matchmaking.Matchmaking.Create(newMatchmakingId, aggregateVersion, settings);
                if (!matchmakingResult.IsOk)
                    throw new JoiningQuickMatchmakingFailedException(command.Nick,
                        JoiningQuickMatchmakingFailReason.ErrorDuringSettingUp);

                var (matchmaking, events) = matchmakingResult.ResultValue;

                var expectedVersion = matchmaking.Version_;
                await
                    matchmakings.SaveAsync(matchmaking.Id_, events, expectedVersion, messageContext.CorrelationId,
                            messageContext.CausationId,
                            ct)
                        .AwaitOrWrap(_ => new JoiningQuickMatchmakingFailedException(command.Nick,
                            JoiningQuickMatchmakingFailReason.ErrorDuringSettingUp));
                return matchmaking.Id_;
            }
        }
    }
}