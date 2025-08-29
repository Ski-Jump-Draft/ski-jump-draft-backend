namespace App.Application._2.Utility;

public interface IRandom
{
    int NextInt();
    int RandomInt(int min, int max);
    double NextDouble();
    double RandomDouble(double min, double max);
}