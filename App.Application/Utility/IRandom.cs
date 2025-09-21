namespace App.Application.Utility;

public interface IRandom
{
    /// <summary>
    /// Zero to max (exclusive)
    /// </summary>
    /// <param name="max"></param>
    /// <returns></returns>
    int Next(int max);
    
    /// <summary>
    /// Całe int >=0
    /// </summary>
    /// <returns></returns>
    int NextInt();
    
    /// <summary>
    /// [min, max)
    /// </summary>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <returns></returns>
    int RandomInt(int min, int max);
    
    /// <summary>
    /// [0, 1]
    /// </summary>
    /// <returns></returns>
    double NextDouble();
    
    /// <summary>
    /// [min, max)
    /// </summary>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <returns></returns>
    double RandomDouble(double min, double max);
}