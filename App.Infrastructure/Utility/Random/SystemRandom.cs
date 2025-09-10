using App.Application.Utility;

namespace App.Infrastructure.Utility.Random;

public class SystemRandom : IRandom
{
    private readonly System.Random _rnd = new();

    /// <summary>
    /// Zero to max (exclusive)
    /// </summary>
    /// <param name="max"></param>
    /// <returns></returns>
    public int Next(int max)
    {
        return _rnd.Next(max);
    }

    public int NextInt() =>
        _rnd.Next(); // całe int >=0

    /// <summary>
    /// Min inclusive, max exclusive
    /// </summary>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <returns></returns>
    public int RandomInt(int min, int max) =>
        _rnd.Next(min, max);

    /// <summary>
    /// [0.0, 1.0]
    /// </summary>
    /// <returns></returns>
    public double NextDouble() =>
        _rnd.NextDouble();

    /// <summary>
    /// Min inclusive, max exclusive
    /// </summary>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <returns></returns>
    public double RandomDouble(double min, double max) =>
        min + _rnd.NextDouble() * (max - min);
}