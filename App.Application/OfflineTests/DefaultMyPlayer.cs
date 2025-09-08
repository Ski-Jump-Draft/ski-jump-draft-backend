namespace App.Application.OfflineTests;

public class DefaultMyPlayer : IMyPlayer
{
    private Guid? _matchmakingPlayerId;
    private Guid? _gamePlayerId;
    private Guid? _matchmakingId;
    private Guid? _gameId;

    public Guid? GetMatchmakingId() => _matchmakingId;

    public Guid? GetGameId() => _gameId;

    public Guid? GetMatchmakingPlayerId() => _matchmakingPlayerId;

    public Guid? GetGamePlayerId() => _gamePlayerId;

    public void SetMatchmakingId(Guid? id) => _matchmakingId = id;

    public void SetGameId(Guid? id) => _gameId = id;

    public void SetMatchmakingPlayerId(Guid? id) => _matchmakingPlayerId = id;

    public void SetGamePlayerId(Guid? id) => _gamePlayerId = id;

    public string GetNick()
    {
        return "SiekamCebulę";
    }
}