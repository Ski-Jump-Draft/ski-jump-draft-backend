using App.Application.Extensions;
using App.Application.Utility;
using App.Domain.Game;

namespace App.Application.Policy.DraftPicker;

public class RandomPicker(IRandom random) : IDraftPicker, IDraftPassPicker
{
    public Task<Guid> Pick(Domain.Game.Game game, CancellationToken ct)
    {
        if (!game.StatusTag.IsDraftTag)
        {
            throw new GameNotInDraftException();
        }

        var availablePicks = game.AvailableDraftPicks.ToList();
        return Task.FromResult(availablePicks.GetRandomElement(random).Item);
    }
}

public class GameNotInDraftException(string? message = null) : Exception(message);