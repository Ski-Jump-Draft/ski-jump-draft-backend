using System.Security.Cryptography;
using System.Text;
using App.Application.Utility;
using Convert = System.Convert;

namespace App.Web.Security;

public class HmacPlayerTokenService(string secret, IMyLogger logger) : IPlayerTokenService
{
    private readonly byte[] _key = string.IsNullOrWhiteSpace(secret)
        ? RandomNumberGenerator.GetBytes(32)
        : Encoding.UTF8.GetBytes(secret);

    public string SignMatchmaking(Guid matchmakingId, Guid playerId)
        => Sign("mm", matchmakingId, playerId);

    public bool VerifyMatchmaking(Guid matchmakingId, Guid playerId, string token)
    {
        logger.Info($"Verifying {matchmakingId}/{playerId}: token prefix {token[..10]}...");
        return Verify("mm", matchmakingId, playerId, token);
    }

    public string SignGame(Guid gameId, Guid playerId)
        => Sign("game", gameId, playerId);

    public bool VerifyGame(Guid gameId, Guid playerId, string token)
        => Verify("game", gameId, playerId, token);

    private string Sign(string scope, Guid id1, Guid id2)
    {
        var payload = $"{scope}:{id1:D}:{id2:D}";
        using var hmac = new HMACSHA256(_key);
        var sig = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Base64UrlEncode(sig);
    }

    private bool Verify(string scope, Guid id1, Guid id2, string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;
        var expected = Sign(scope, id1, id2);
        try
        {
            var a = Base64UrlDecode(expected);
            var b = Base64UrlDecode(token);
            return CryptographicOperations.FixedTimeEquals(a, b);
        }
        catch
        {
            return false;
        }
    }

    private static string Base64UrlEncode(byte[] data)
    {
        return Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string s)
    {
        s = s.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }

        return Convert.FromBase64String(s);
    }
}