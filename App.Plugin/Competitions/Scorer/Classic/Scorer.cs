using App.Domain.Competition;
using App.Domain.Competition.Jump;
using App.Domain.Competition.Results;
using ResultObjects = App.Domain.Competition.Results;
using static Microsoft.FSharp.Collections.ListModule;
using Abstractions = App.Domain.Competition.Results.Abstractions;

namespace App.Plugin.Competitions.Scorer.Classic;

public sealed class ClassicJumpScorer(
    Abstractions.IWindPointsGrantor windPointsGrantor,
    Abstractions.IGatePointsGrantor gatePointsGrantor,
    Abstractions.IStylePointsAggregator stylePointsAggregator
)
    : Abstractions.IJumpScorer
{
    public ResultObjects.JumpScore Evaluate(Jump jump)
    {
        var distance = DistanceModule.value(jump.Distance);
        var kPoint = HillModule.KPointModule.value(jump.KPoint);
        var hsPoint = HillModule.HSPointModule.value(jump.HSPoint);
        var hillType = ClassifyByHs(hsPoint);

        var distancePoints = PointsPerKPoint(hillType) + ((distance - kPoint) * PointsPerMeter(kPoint));

        var dummyMarks = OfSeq([
            Judgement.JudgeMarkModule.tryCreate(15).ResultValue,
            Judgement.JudgeMarkModule.tryCreate(15.5).ResultValue,
            Judgement.JudgeMarkModule.tryCreate(16.25).ResultValue,
            Judgement.JudgeMarkModule.tryCreate(18).ResultValue
        ]);
        var marksList = Judgement.JudgeMarksList.NewJudgeMarksList(dummyMarks);

        var total = distancePoints;
        total += GetStylePoints(marksList, out var stylePoints);
        total += GetWindPoints(jump, out var windPoints);
        total += GetGatePoints(jump, out var gatePoints);

        return new ResultObjects.JumpScore(
            points: ResultObjects.JumpScoreModule.TotalPoints.NewTotalPoints(total),
            stylePoints: stylePoints,
            windPoints: windPoints,
            gatePoints: gatePoints
        );
    }

    private double GetStylePoints(Judgement.JudgeMarksList marksList,
        out ResultObjects.JumpScoreModule.StylePoints stylePoints)
    {
        stylePoints = stylePointsAggregator.Aggregate(marksList);
        var pts = stylePoints is ResultObjects.JumpScoreModule.StylePoints.CustomValue custom
            ? custom.Points
            : ((ResultObjects.JumpScoreModule.StylePoints.SumOfSelectedMarks)stylePoints).Points;
        return pts;
    }

    private double GetWindPoints(Jump jump, out ResultObjects.JumpScoreModule.WindPoints windPoints)
    {
        windPoints = windPointsGrantor.Grant(jump);
        if (windPoints.IsNone)
            return 0;

        return ((ResultObjects.JumpScoreModule.WindPoints.Some)windPoints).Item;
    }

    private double GetGatePoints(Jump jump, out ResultObjects.JumpScoreModule.GatePoints gatePoints)
    {
        gatePoints = gatePointsGrantor.Grant(jump);
        if (gatePoints.IsNone)
            return 0;

        return ((ResultObjects.JumpScoreModule.GatePoints.Some)gatePoints).Item;
    }

    private static double PointsPerMeter(double kPoint)
    {
        return kPoint switch
        {
            < 25.0 => 4.8,
            < 30.0 => 4.4,
            < 35.0 => 4.0,
            < 40.0 => 3.6,
            < 50.0 => 3.2,
            < 60.0 => 2.8,
            < 70.0 => 2.4,
            < 80.0 => 2.2,
            < 100.0 => 2.0,
            < 135.0 => 1.8,
            < 180.0 => 1.6,
            _ => 1.2
        };
    }

    private static double PointsPerKPoint(HillType type) =>
        type switch
        {
            HillType.SkiFlying => 120,
            _ => 60
        };

    public enum HillType
    {
        Small,
        Medium,
        Normal,
        Large,
        Big,
        SkiFlying
    }

    private static HillType ClassifyByHs(double hs)
    {
        return hs switch
        {
            < 50.0 => HillType.Small,
            < 85.0 => HillType.Medium,
            < 110.0 => HillType.Normal,
            < 150.0 => HillType.Large,
            < 185.0 => HillType.Big,
            _ => HillType.SkiFlying
        };
    }
}