namespace App.Application.Utility;

public interface IClock
{
    DateTime UtcNow();
    DateTimeOffset Now();
}