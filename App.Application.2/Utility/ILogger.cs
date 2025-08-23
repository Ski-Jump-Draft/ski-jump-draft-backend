namespace App.Application._2.Utility;

public interface ILogger
{
    void Info(string message);
    void Warn(string message, Exception? ex = null);
    void Error(string message, Exception? ex = null);
}