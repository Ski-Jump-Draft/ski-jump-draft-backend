using App.Domain.Game;

namespace App.Application.Game.DraftPicks;

using Picks = Dictionary<PlayerId, IEnumerable<JumperId>>;

/// <summary>
/// Archives ended game drafts
/// </summary>
public interface IDraftPicksArchive
{
    Task Archive(Guid gameId, Picks picks);
    Task<Picks?> GetPicks(Guid gameId);
}