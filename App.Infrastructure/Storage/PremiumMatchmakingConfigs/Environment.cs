using System.Security.Cryptography;
using System.Text;
using App.Application.Matchmaking;

namespace App.Infrastructure.Storage.PremiumMatchmakingConfigs;

public class Environment(string? premiumPasswordsString) : IPremiumMatchmakingConfigurationStorage
{
    public Task<HashSet<PremiumMatchmakingConfig>> PremiumMatchmakingConfigs => Task.FromResult(SetUp());

    private HashSet<PremiumMatchmakingConfig> SetUp()
    {
        var set = new HashSet<PremiumMatchmakingConfig>();
        if (string.IsNullOrWhiteSpace(premiumPasswordsString))
        {
            return set;
        }

        foreach (var item in premiumPasswordsString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            set.Add(new PremiumMatchmakingConfig(item));
        }

        return set;
    }

    public async Task<PremiumMatchmakingConfig?> GetByPassword(string password)
    {
        var configs = await PremiumMatchmakingConfigs;
        return configs.FirstOrDefault(config => SecureEquals(config.Password, password));
    }

    private static bool SecureEquals(string a, string b)
    {
        var ba = Encoding.UTF8.GetBytes(a);
        var bb = Encoding.UTF8.GetBytes(b);
        if (ba.Length != bb.Length)
        {
            var max = Math.Max(ba.Length, bb.Length);
            Array.Resize(ref ba, max);
            Array.Resize(ref bb, max);
        }

        return CryptographicOperations.FixedTimeEquals(ba, bb);
    }
}