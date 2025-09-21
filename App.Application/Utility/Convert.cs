namespace App.Application.Utility;

public static class Convert
{
    public static int? ToSeconds(this Domain.Game.DraftModule.SettingsModule.TimeoutPolicy timeoutPolicy)
    {
        return timeoutPolicy switch
        {
            Domain.Game.DraftModule.SettingsModule.TimeoutPolicy.TimeoutAfter timeoutAfter => (int)Math.Floor(
                timeoutAfter
                    .Time.TotalSeconds),
            { IsNoTimeout: true } => null,
            _ => throw new InvalidOperationException($"Unknown DraftSettings.TimeoutPolicy: {
                timeoutPolicy}")
        };
    }
}