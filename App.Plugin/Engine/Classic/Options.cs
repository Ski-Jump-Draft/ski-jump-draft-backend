using App.Domain.Competition.Results;
using App.Domain.Competition.Rules;

namespace App.Plugin.Engine.Classic;

using Application.Commanding;
using App.Domain.Competition;

public abstract record RoundParticipantsLimit
{
    private RoundParticipantsLimit()
    {
    }

    public sealed record Exact(int Limit) : RoundParticipantsLimit;

    public sealed record Soft(int Limit) : RoundParticipantsLimit;

    public sealed record None() : RoundParticipantsLimit
    {
        public static readonly None Instance = new();
    }
}

// public sealed record StylePolicy(
//     string Kind, // "DropHighAndLowMarks" | "TopNMarksSum" | "AverageOfMarks"
//     Dictionary<string, string> Args // np. { "n":"8" }
// );

public sealed record Options : Domain.Competition.Engine.IOptions
{
    public bool WindPointsEnabled { get; }
    public bool GatePointsEnabled { get; }
    public bool StylePointsEnabled { get; }
    public double? PointsPerGate { get; }
    public double? HeadwindPoints { get; }
    public double? TailwindPoints { get; }
    public List<RoundParticipantsLimit> RoundLimits { get; }
    public CompetitionCategory Category { get; }
    //public StylePolicy StylePolicy { get; }

    public Options(
            bool windPointsEnabled,
            bool gatePointsEnabled,
            bool stylePointsEnabled,
            double? pointsPerGate,
            double? headwindPoints,
            double? tailwindPoints, List<RoundParticipantsLimit> roundLimits, CompetitionCategory category)
        //StylePolicy stylePolicy)
    {
        if (windPointsEnabled && (headwindPoints is null || tailwindPoints is null))
            throw new ArgumentException("headwindPoints and tailwindPoints must not be null when wind is enabled");

        if (gatePointsEnabled && pointsPerGate is null or <= 0)
            throw new ArgumentException("pointsPerGate must be > 0 when gate is enabled", nameof(pointsPerGate));

        WindPointsEnabled = windPointsEnabled;
        GatePointsEnabled = gatePointsEnabled;
        StylePointsEnabled = stylePointsEnabled;
        PointsPerGate = pointsPerGate;
        HeadwindPoints = headwindPoints;
        TailwindPoints = tailwindPoints;
        RoundLimits = roundLimits;
        Category = category;
        //StylePolicy = stylePolicy;
    }
}