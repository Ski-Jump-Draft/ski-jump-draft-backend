namespace App.Application.Utility;

public interface IMyLogger
{
    void Info(string message);
    void Debug(string message);
    void Warn(string message, Exception? ex = null);
    void Error(string message, Exception? ex = null);
}