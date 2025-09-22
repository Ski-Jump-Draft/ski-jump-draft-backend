using App.Application.Exceptions;
using App.Application.Extensions;
using App.Domain.Game;

namespace App.Application.Policy.GameCompetitionStartlist;

public class All(IGames games) : IGameCompetitionStartlist
{
    public async Task<IReadOnlyList<JumperId>> Get(Guid gameId, GameCompetitionDto competition, CancellationToken ct)
    {
        var game = await games.GetById(GameId.NewGameId(gameId), ct).AwaitOrWrap(_ => new IdNotFoundException(gameId));
        return JumpersModule.toIdsList(game.Jumpers);
    }
}