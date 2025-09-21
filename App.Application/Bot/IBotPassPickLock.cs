namespace App.Application.Bot;

/// <summary>
/// Is used to lock pass pick possibility, when bot picks
/// </summary>
public interface IBotPassPickLock
{
    void Lock(Guid gameId, Guid playerId);
    void Unlock(Guid gameId, Guid playerId);
    bool IsLocked(Guid gameId, Guid playerId);
}