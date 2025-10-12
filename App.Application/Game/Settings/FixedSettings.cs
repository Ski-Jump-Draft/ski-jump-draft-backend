namespace App.Application.Game.Settings;

public class FixedSettings(Domain.Game.Settings settings) : IGameSettingsFactory
{
    public Domain.Game.Settings Create()
    {
        return settings;
    }
}