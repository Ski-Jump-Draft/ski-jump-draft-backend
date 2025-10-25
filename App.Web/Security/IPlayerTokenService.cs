using System.Security.Cryptography;

namespace App.Web.Security;

public interface IPlayerTokenService
{
    string SignMatchmaking(Guid matchmakingId, Guid playerId);
    bool VerifyMatchmaking(Guid matchmakingId, Guid playerId, string token);
    string SignGame(Guid gameId, Guid playerId);
    bool VerifyGame(Guid gameId, Guid playerId, string token);
}
