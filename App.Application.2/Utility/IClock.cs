namespace App.Application._2.Utility;

public interface IClock
{
    DateTime UtcNow();
    DateTimeOffset Now();
}