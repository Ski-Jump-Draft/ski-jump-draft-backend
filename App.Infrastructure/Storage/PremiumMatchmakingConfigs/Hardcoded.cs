using App.Application.Matchmaking;

namespace App.Infrastructure.Storage.PremiumMatchmakingConfigs;

public class Hardcoded : IPremiumMatchmakingConfigurationStorage
{
    public Task<ISet<PremiumMatchmakingConfig>> PremiumMatchmakingConfigs =>
        Task.FromResult<ISet<PremiumMatchmakingConfig>>(new HashSet<PremiumMatchmakingConfig>()
        {
            new("sjdraft123"),
            new(Password: "siekamy cebulkę")
        });

    public async Task<PremiumMatchmakingConfig?> GetByPassword(string password)
    {
        var configs = await PremiumMatchmakingConfigs;
        return configs.FirstOrDefault(config => config.Password == password);
    }
}