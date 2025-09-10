using App.Application.JumpersForm;

namespace App.Infrastructure.Storage.JumperGameForm;

public class InMemory : IJumperGameFormStorage
{
    private readonly Dictionary<Guid, double> _store = new();

    public double GetGameForm(Guid gameJumperId)
        => _store.TryGetValue(gameJumperId, out var form)
            ? form
            : throw new KeyNotFoundException($"No form for {gameJumperId}");

    public double Add(Guid gameJumperId, double form)
    {
        _store[gameJumperId] = form;
        return form;
    }

    public double Remove(Guid gameJumperId)
    {
        if (_store.Remove(gameJumperId, out var form))
        {
            return form;
        }

        throw new KeyNotFoundException($"No form to remove for {gameJumperId}");
    }
}