using App.Domain.Game;

namespace App.Application.Policy.DraftPicker;

public interface IDraftPicker
{
    Task<Guid> Pick(Domain.Game.Game game, CancellationToken ct);
}

public interface IDraftPassPicker : IDraftPicker
{
}