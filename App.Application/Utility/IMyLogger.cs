namespace App.Application.Utility;

public interface IMyLogger
{
    void Info(string message, object? data = null);
    void Debug(string message, object? data = null);
    void Warn(string message, Exception? ex = null, object? data = null);
    void Error(string message, Exception? ex = null, object? data = null);
}