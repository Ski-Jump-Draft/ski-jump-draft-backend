using System.Security.Cryptography;
using System.Text;
using App.Application.Matchmaking;

namespace App.Infrastructure.Storage.PremiumMatchmakingConfigs;

public class Environment : IPremiumMatchmakingConfigurationStorage
{
    public Task<ISet<PremiumMatchmakingConfig>> PremiumMatchmakingConfigs => Task.FromResult(LoadFromEnvironment());

    private static ISet<PremiumMatchmakingConfig> LoadFromEnvironment()
    {
        // Read from environment variable to avoid hardcoding secrets
        var raw = System.Environment.GetEnvironmentVariable("PREMIUM_PASSWORDS");
        var set = new HashSet<PremiumMatchmakingConfig>();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return set;
        }

        foreach (var item in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            set.Add(new PremiumMatchmakingConfig(item));
        }
        return set;
    }

    public async Task<PremiumMatchmakingConfig?> GetByPassword(string password)
    {
        var configs = await PremiumMatchmakingConfigs;
        foreach (var config in configs)
        {
            if (SecureEquals(config.Password, password))
            {
                return config;
            }
        }
        return null;
    }

    private static bool SecureEquals(string a, string b)
    {
        // Fixed-time comparison to mitigate timing attacks
        var ba = Encoding.UTF8.GetBytes(a);
        var bb = Encoding.UTF8.GetBytes(b);
        if (ba.Length != bb.Length)
        {
            // Compare anyway to keep timing similar
            var max = Math.Max(ba.Length, bb.Length);
            Array.Resize(ref ba, max);
            Array.Resize(ref bb, max);
        }
        return CryptographicOperations.FixedTimeEquals(ba, bb);
    }
}