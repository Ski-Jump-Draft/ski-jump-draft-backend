using App.Domain.Competition.Jump;
using App.Domain.Competition.Results;
using App.Domain.Competition.Results.ResultObjects;
using Results_Abstractions = App.Domain.Competition.Results.Abstractions;

namespace App.Plugin.Competitions.GatePointsGrantor;

public class NoGatePointsGrantor : Results_Abstractions.IGatePointsGrantor
{
    public JumpScoreModule.GatePoints Grant(Jump _) => JumpScoreModule.GatePoints.None;
}