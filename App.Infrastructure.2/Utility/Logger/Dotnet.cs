using Microsoft.Extensions.Logging;
using ILogger = App.Application._2.Utility.ILogger;

namespace App.Infrastructure._2.Utility.Logger;

public class Dotnet(ILoggerFactory factory) : ILogger
{
    private readonly Microsoft.Extensions.Logging.ILogger _inner =
        factory.CreateLogger("App"); // kategoria globalna, możesz dać dynamicznie

    public void Info(string message) => _inner.LogInformation(message);
    public void Warn(string message, Exception? ex = null) => _inner.LogWarning(ex, message);
    public void Error(string message, Exception? ex = null) => _inner.LogError(ex, message);
}