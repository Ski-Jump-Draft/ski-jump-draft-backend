using App.Domain.Competition;
using App.Domain.Competition.Jump;
using App.Domain.Competition.Results;
using App.Domain.Competition.Results;
using static Microsoft.FSharp.Collections.ListModule;
using IStylePointsAggregator = App.Domain.Competition.Results.Abstractions.IStylePointsAggregator;

namespace App.Plugin.Competitions.JudgesPointsAggregator;

public sealed class DropHighAndLowMarks : IStylePointsAggregator
{
    public JumpScoreModule.StylePoints Aggregate(Judgement.JudgeMarksList judgeMarks)
    {
        var marks = judgeMarks.Item;
        var rawMarks = marks.Select(Judgement.JudgeMarkModule.value).ToList();
        if (rawMarks.Count <= 2)
            return JumpScoreModule.StylePoints.NewSumOfSelectedMarks(judgeMarks, rawMarks.Sum());

        var marksWithoutMinAndMax = marks.Where(m => m != marks.Min() && m != marks.Max()).ToList();
        var sumWithoutMinAndMax = rawMarks.Sum() - rawMarks.Max() - rawMarks.Min();

        return JumpScoreModule.StylePoints.NewSumOfSelectedMarks(
            Judgement.JudgeMarksList.NewJudgeMarksList(OfSeq(marksWithoutMinAndMax)), sumWithoutMinAndMax);
    }
}

public sealed class TopNMarksSum(int n) : IStylePointsAggregator
{
    public JumpScoreModule.StylePoints Aggregate(Judgement.JudgeMarksList judgeMarks)
    {
        var marks = judgeMarks.Item;
        var topNMarks = marks.OrderByDescending(v => v).Take(n).ToList();
        var sum = topNMarks.Select(Judgement.JudgeMarkModule.value).Sum();
        return JumpScoreModule.StylePoints.NewSumOfSelectedMarks(
            Judgement.JudgeMarksList.NewJudgeMarksList(OfSeq(topNMarks)), sum);
    }
}

public sealed class AverageOfMarks : IStylePointsAggregator
{
    public JumpScoreModule.StylePoints Aggregate(Judgement.JudgeMarksList judgeMarks)
    {
        var marks = judgeMarks.Item;
        var average = marks.Select(Judgement.JudgeMarkModule.value).Average();
        return JumpScoreModule.StylePoints.NewCustomValue(average);
    }
}

public sealed class NoPoints : IStylePointsAggregator
{
    public JumpScoreModule.StylePoints Aggregate(Judgement.JudgeMarksList judgeMarks) =>
        JumpScoreModule.StylePoints.None;
}