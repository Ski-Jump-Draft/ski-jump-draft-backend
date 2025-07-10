using App.Domain.Competition.Results.ResultObjects;
using App.Domain.Competition.Jump;
using App.Plugin.Competitions.WindAggregator;
using Abstractions = App.Domain.Competition.Results.Abstractions;

namespace App.Plugin.Competitions.WindPointsGrantor;

public class ClassicWindPointsGrantor(double headwindPoints, double tailwindPoints) : Abstractions.IWindPointsGrantor
{
    private readonly Domain.Competition.Jump.Abstractions.IWindAggregator
        _windAggregator = new MockWindAggregator(0.33); // TODO

    public JumpScoreModule.WindPoints Grant(Jump jump)
    {
        var aggregatedWind = _windAggregator.Aggregate(jump.WindMeasurement);

        if (aggregatedWind.IsHeadwind)
            return JumpScoreModule.WindPoints.NewSome(-headwindPoints * aggregatedWind.Item);
        if (aggregatedWind.IsTailwind)
            return JumpScoreModule.WindPoints.NewSome(-tailwindPoints * aggregatedWind.Item);
        return JumpScoreModule.WindPoints.None;
    }
}