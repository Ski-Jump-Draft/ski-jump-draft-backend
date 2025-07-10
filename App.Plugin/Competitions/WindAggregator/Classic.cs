using App.Domain.Competition.Jump;

namespace App.Plugin.Competitions.WindAggregator;

// TODO
public class ClassicWindAggregator : Abstractions.IWindAggregator
{
    public Wind.AggregatedWind Aggregate(Wind.WindMeasurement windMeasurement)
    {
        throw new NotImplementedException();
    }
}