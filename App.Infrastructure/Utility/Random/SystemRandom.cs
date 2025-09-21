using App.Application.Utility;

namespace App.Infrastructure.Utility.Random;

public class SystemRandom : IRandom
{
    private readonly System.Random _rnd = new();

    public int Next(int max)
    {
        return _rnd.Next(max);
    }

    public int NextInt() =>
        _rnd.Next(); // całe int >=0

    public int RandomInt(int min, int max) =>
        _rnd.Next(min, max);

    public double NextDouble() =>
        _rnd.NextDouble();

    public double RandomDouble(double min, double max) =>
        min + _rnd.NextDouble() * (max - min);
}