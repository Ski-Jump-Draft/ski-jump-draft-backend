namespace App.Application._2.Policy.GameHillSelector;

public interface IGameHillSelector
{
    Task<Guid> Select(CancellationToken ct);
}