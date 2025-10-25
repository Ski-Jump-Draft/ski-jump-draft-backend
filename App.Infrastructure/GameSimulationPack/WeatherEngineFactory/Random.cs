using App.Application.Game.GameSimulationPack.WeatherEngineFactory;
using App.Application.Utility;
using App.Domain.Simulation;
using App.Simulator.Simple;

namespace App.Infrastructure.GameSimulationPack.WeatherEngineFactory;

public class Random(IRandom random, IMyLogger logger) : IWeatherEngineFactory
{
    public IWeatherEngine Create()
    {
        var startingWind = SampleStartingWind(random);

        var stableWindChangeStdDev = SampleStableStdDevTargeted(random);      // mean ~ 0.05
        var windAdditionStdDev     = SampleWindAdditionStdDev_Mixture(random); // częściej duże, ale nadal spoko

        stableWindChangeStdDev = Math.Max(stableWindChangeStdDev, 1e-6);
        windAdditionStdDev     = Math.Max(windAdditionStdDev, 1e-6);

        var configuration = new Configuration(startingWind, stableWindChangeStdDev, windAdditionStdDev);
        logger.Info($"Creating a weather engine with configuration: {configuration}");
        return new WeatherEngine(random, logger, configuration);
    }

    // --- STARTING WIND: mieszanka 4N(0,σ), jeden komponent lekko "ucięty" ---
    private static double SampleStartingWind(IRandom random)
    {
        // Wagi: low=0.245 (σ=0.65), mid1=0.285 (σ=0.80), mid2=0.29 (σ=0.95, trunc |w|<=1.9), high=0.18 (σ=1.25)
        var u = random.NextDouble();
        if (u < 0.18)
        {
            // high
            return random.Gaussian(0, 1.25);
        }
        u -= 0.18;
        if (u < 0.29)
        {
            // mid2 (σ=0.95) - truncation |w| <= 1.9 (akceptacja ~97–98%, więc pętla szybka)
            double x;
            do { x = random.Gaussian(0, 0.95); } while (Math.Abs(x) > 1.9);
            return x;
        }
        u -= 0.29;
        if (u < 0.285)
        {
            // mid1
            return random.Gaussian(0, 0.80);
        }
        // low
        return random.Gaussian(0, 0.65);
    }

    // --- STABLE: Beta(2,12) ze skalą 0.35 => mean ≈ 0.35 * 2/14 = 0.05 ---
    private static double SampleStableStdDevTargeted(IRandom random)
    {
        var x = SampleBetaInt(random, 2, 12);
        return 0.35 * x;
    }

    // --- ADDITION: mieszanka "starego" kształtu i grubszego ogona ---
    private static double SampleWindAdditionStdDev_Mixture(IRandom random)
    {
        var heavy = random.NextDouble() < 0.15;
        var x = heavy ? SampleBetaInt(random, 2, 4)
                      : SampleBetaInt(random, 2, 10);
        return 1.2 * x;
    }

    // --- Narzędzia Beta/Exp (jak miałeś) ---
    private static double SampleBetaInt(IRandom random, int a, int b)
    {
        double x = 0.0, y = 0.0;
        for (var i = 0; i < a; i++) x += SampleExp1(random);
        for (var j = 0; j < b; j++) y += SampleExp1(random);
        return x / (x + y);
    }

    private static double SampleExp1(IRandom random)
    {
        double u;
        do u = random.NextDouble(); while (u <= 0.0);
        return -Math.Log(u);
    }
}
