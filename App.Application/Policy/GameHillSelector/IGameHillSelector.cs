namespace App.Application.Policy.GameHillSelector;

public interface IGameHillSelector
{
    Task<Guid> Select(CancellationToken ct);
}