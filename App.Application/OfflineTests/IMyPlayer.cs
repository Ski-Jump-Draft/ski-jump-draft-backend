namespace App.Application.OfflineTests;

public interface IMyPlayer
{
    Guid? GetMatchmakingId();
    Guid? GetGameId();
    Guid? GetMatchmakingPlayerId();
    Guid? GetGamePlayerId();    
    void SetMatchmakingId(Guid? id);
    void SetGameId(Guid? id);
    void SetMatchmakingPlayerId(Guid? id);
    void SetGamePlayerId(Guid? id);
    string GetNick();
}