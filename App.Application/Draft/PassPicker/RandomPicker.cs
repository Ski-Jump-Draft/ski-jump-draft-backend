using App.Application.Extensions;
using App.Application.Utility;
using App.Domain.Game;

namespace App.Application.Draft.PassPicker;

public class RandomPicker(IRandom random) : IDraftPassPicker
{
    public JumperId Pick(Domain.Game.Game game)
    {
        if (!game.StatusTag.IsDraftTag)
        {
            throw new GameNotInDraftException();
        }

        var availablePicks = game.AvailableDraftPicks.ToList();
        return availablePicks.GetRandomElement(random);
    }
}

public class GameNotInDraftException(string? message = null) : Exception(message);
