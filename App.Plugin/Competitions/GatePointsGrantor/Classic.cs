using App.Domain.Competition;
using App.Domain.Competition.Jump;
using App.Domain.Competition.Results;
using App.Domain.Competition.Results;
using IGatePointsGrantor = App.Domain.Competition.Results.Abstractions.IGatePointsGrantor;

namespace App.Plugin.Competitions.GatePointsGrantor;

public enum CoachGatePointsPolicy
{
    AlwaysGrant,
    RequireHsPercent,
}

public class ClassicGatePointsGrantor : IGatePointsGrantor
{
    private readonly double _pointsPerGate;
    private readonly CoachGatePointsPolicy _coachGatePointsPolicy;
    private readonly double? _requiredHsPercentForCoachGatePoints;

    public ClassicGatePointsGrantor(
        double pointsPerGate,
        CoachGatePointsPolicy coachGatePointsPolicy,
        double? requiredHsPercentForCoachGatePoints)
    {
        if (coachGatePointsPolicy == CoachGatePointsPolicy.RequireHsPercent)
        {
            if (requiredHsPercentForCoachGatePoints is null or <= 0)
                throw new ArgumentException(
                    "requiredHsPercentForCoachGatePoints must be > 0 when policy is RequireHsPercent");
        }

        _pointsPerGate = pointsPerGate;
        _coachGatePointsPolicy = coachGatePointsPolicy;
        _requiredHsPercentForCoachGatePoints = requiredHsPercentForCoachGatePoints;
    }

    public JumpScoreModule.GatePoints Grant(Jump jump)
    {
        var gateStatus = jump.GateStatus;
        if (!gateStatus.IsSome) return NoneGatePoints();

        var realGateStatus = (Gate.GateStatus.Some)gateStatus;
        var coachStatus = realGateStatus.CoachStatus;
        if (!coachStatus.IsLoweredGate) return NoneGatePoints();

        var loweredGateStatus = (Gate.CoachStatus.LoweredGate)coachStatus;
        if (_coachGatePointsPolicy != CoachGatePointsPolicy.RequireHsPercent)
            return FullGatePoints(realGateStatus);

        var hs = HillModule.HsPointModule.value(jump.HsPoint);
        var distance = DistanceModule.value(jump.Distance);
        var requiredDistance = hs * _requiredHsPercentForCoachGatePoints!.Value;

        if (distance >= requiredDistance)
            return FullGatePoints(realGateStatus);

        var loweredGates = Gate.CoachStatusModule.LoweredGate.CountModule.value(loweredGateStatus.Count);
        return CoachGateLoweringMissedPoints(realGateStatus, loweredGates);
    }

    private JumpScoreModule.GatePoints NoneGatePoints() => JumpScoreModule.GatePoints.None;

    private JumpScoreModule.GatePoints FullGatePoints(Gate.GateStatus.Some gateStatus)
    {
        var startGate = gateStatus.StartGate.Item;
        var currentGate = gateStatus.CurrentGate.Item;
        return JumpScoreModule.GatePoints.NewSome((startGate - currentGate) * _pointsPerGate);
    }

    private JumpScoreModule.GatePoints CoachGateLoweringMissedPoints(Gate.GateStatus.Some gateStatus,
        int howMuchLoweredByCoach)
    {
        var startGate = gateStatus.StartGate.Item;
        var currentGate = gateStatus.CurrentGate.Item;
        return JumpScoreModule.GatePoints.NewSome((startGate - currentGate + howMuchLoweredByCoach) * _pointsPerGate);
    }
}