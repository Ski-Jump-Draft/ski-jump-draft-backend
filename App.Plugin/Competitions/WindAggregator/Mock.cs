using App.Domain.Competition.Jump;

namespace App.Plugin.Competitions.WindAggregator;

public class MockWindAggregator(double defaultValue) : Abstractions.IWindAggregator
{
    public Wind.AggregatedWind Aggregate(Wind.WindMeasurement windMeasurement)
    {
        if (windMeasurement.IsSingle)
        {
            return Wind.AggregatedWind.NewAggregatedWind(((Wind.WindMeasurement.Single)windMeasurement).Item.StrengthMs.Item);       
        }
        return Wind.AggregatedWind.NewAggregatedWind(defaultValue);
    }
}