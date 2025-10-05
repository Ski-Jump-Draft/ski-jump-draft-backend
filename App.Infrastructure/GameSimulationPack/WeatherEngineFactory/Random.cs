using App.Application.Game.GameSimulationPack.WeatherEngineFactory;
using App.Application.Utility;
using App.Domain.Simulation;
using App.Simulator.Simple;

namespace App.Infrastructure.GameSimulationPack.WeatherEngineFactory;

public class Random(IRandom random, IMyLogger logger) : IWeatherEngineFactory
{
    public IWeatherEngine Create()
    {
        var startingWind = random.RandomInt(0, 10) switch
        {
            <= 1 => random.Gaussian(0, 1.25),
            _ => random.Gaussian(0, 0.65),
        };

        // --- stableWindChangeStdDev ---
        // 80% w [0, 0.05] ~ Beta(2,6) (gęsto przy 0)
        // 20% w [0.05, 0.3], silne skupienie blisko 0.05;
        // w tej 20% gałęzi ~1/400 szansa na okolice [0.28, 0.30].
        var stableWindChangeStdDev = SampleStableStdDev(random);

        // --- windAdditionStdDev ---
        // Beta(2,6) przeskalowana do [0, 1.5] -> moda dokładnie przy 0.25
        var windAdditionStdDev = SampleWindAdditionStdDev(random);

        // minimalne >0, żeby spełnić wymaganie
        stableWindChangeStdDev = Math.Max(stableWindChangeStdDev, 1e-6);
        windAdditionStdDev = Math.Max(windAdditionStdDev, 1e-6);

        var configuration = new Configuration(startingWind, stableWindChangeStdDev, windAdditionStdDev);
        
        logger.Info($"Creating a weather engine with configuration: {configuration}");
        return new WeatherEngine(random, logger, configuration);
    }

    private static double SampleStableStdDev(IRandom random)
    {
        var x = SampleBetaInt(random, 2, 12);
        return 0.26 * x;
    }

    private static double SampleWindAdditionStdDev(IRandom random)
    {
        // Beta(2,6) na [0,1] ma modę (2-1)/(2+6-2) = 1/6
        // Skalujemy do [0,1.5] -> moda = 1.5 * 1/6 = 0.25 (idealnie jak chcemy)
        var x = SampleBetaInt(random, 2, 6);
        return 1.3 * x;
    }

    // --- Narzędzia ---

    // Beta(k, m) dla CAŁKOWITYCH k, m: X=sum_{i=1..k}Exp(1), Y=sum_{j=1..m}Exp(1), X/(X+Y)
    private static double SampleBetaInt(IRandom random, int a, int b)
    {
        double x = 0.0, y = 0.0;
        for (var i = 0; i < a; i++) x += SampleExp1(random);
        for (var j = 0; j < b; j++) y += SampleExp1(random);
        return x / (x + y);
    }

    private static double SampleExp1(IRandom random)
    {
        // Exp(1): -ln(U), z U ~ Unif(0,1)
        double u;
        do
        {
            u = random.NextDouble();
        } while (u <= 0.0); // uniknij ln(0)

        return -Math.Log(u);
    }
}