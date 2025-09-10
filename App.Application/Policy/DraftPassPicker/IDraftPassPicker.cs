using App.Domain.Game;

namespace App.Application.Policy.DraftPassPicker;

public interface IDraftPassPicker
{
    Task<Guid> Pick(Domain.Game.Game game, CancellationToken ct);
}