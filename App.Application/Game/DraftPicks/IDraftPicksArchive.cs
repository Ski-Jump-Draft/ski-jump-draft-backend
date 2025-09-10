using App.Domain.Game;

namespace App.Application.Game.DraftPicks;

using Picks = Dictionary<PlayerId, IEnumerable<JumperId>>;

/// <summary>
/// Archives ended game drafts
/// </summary>
public interface IDraftPicksArchive
{
    void Archive(Guid gameId, Picks picks);
    Picks GetPicks(Guid gameId);
}