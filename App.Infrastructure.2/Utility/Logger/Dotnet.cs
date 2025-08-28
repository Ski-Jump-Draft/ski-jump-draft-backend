using App.Application._2.Utility;
using Microsoft.Extensions.Logging;

namespace App.Infrastructure._2.Utility.Logger;

public class Dotnet(ILoggerFactory factory) : IMyLogger
{
    private readonly Microsoft.Extensions.Logging.ILogger _inner =
        factory.CreateLogger("App"); // kategoria globalna, możesz dać dynamicznie

    public void Info(string message) => _inner.LogInformation(message);
    public void Debug(string message)
    {
        _inner.LogDebug(message);
    }

    public void Warn(string message, Exception? ex = null) => _inner.LogWarning(ex, message);
    public void Error(string message, Exception? ex = null) => _inner.LogError(ex, message);
}