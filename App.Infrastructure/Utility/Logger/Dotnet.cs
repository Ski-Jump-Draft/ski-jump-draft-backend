using App.Application.Utility;
using Microsoft.Extensions.Logging;

namespace App.Infrastructure.Utility.Logger;

public class Dotnet(ILoggerFactory factory) : IMyLogger
{
    private readonly Microsoft.Extensions.Logging.ILogger _inner =
        factory.CreateLogger("App");

    public void Info(string message, object? data = null)
    {
        if (data is null)
            _inner.LogInformation(message);
        else
            _inner.LogInformation("{Message} {@Data}", message, data);
    }

    public void Debug(string message, object? data = null)
    {
        if (data is null)
            _inner.LogDebug(message);
        else
            _inner.LogDebug("{Message} {@Data}", message, data);
    }

    public void Warn(string message, Exception? ex = null, object? data = null)
    {
        if (data is null)
            _inner.LogWarning(ex, message);
        else
            _inner.LogWarning(ex, "{Message} {@Data}", message, data);
    }

    public void Error(string message, Exception? ex = null, object? data = null)
    {
        if (data is null)
            _inner.LogError(ex, message);
        else
            _inner.LogError(ex, "{Message} {@Data}", message, data);
    }
}
