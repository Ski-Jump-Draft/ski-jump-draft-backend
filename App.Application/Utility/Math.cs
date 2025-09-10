namespace App.Application.Utility;

public static class MathExtensions
{
    public static double NthRoot(this double a, double n)
    {
        return Math.Pow(a, 1.0 / n);
    }
}