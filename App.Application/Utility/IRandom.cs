namespace App.Application.Utility;

public interface IRandom
{
    int Next(int max);
    int NextInt();
    int RandomInt(int min, int max);
    double NextDouble();
    double RandomDouble(double min, double max);
}