namespace App.Application.Utility;

public static class RandomDistributions
{
    public static double Gaussian(this IRandom random, double mean, double stdDev)
    {
        // Box-Muller transform
        var u1 = 1.0 - random.NextDouble(); // unikaj 0
        var u2 = 1.0 - random.NextDouble();
        var randStdNormal = System.Math.Sqrt(-2.0 * System.Math.Log(u1)) *
                            System.Math.Sin(2.0 * System.Math.PI * u2);

        return mean + stdDev * randStdNormal;
    }
}