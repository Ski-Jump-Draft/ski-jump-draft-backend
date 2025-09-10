using App.Domain.Competition.Jump;
using App.Domain.Competition.Results;
using App.Domain.Competition.Results;
using App.Plugin.Competitions.WindAggregator;
using Results_Abstractions = App.Domain.Competition.Results.Abstractions;

namespace App.Plugin.Competitions.WindPointsGrantor;

public class NoWindPointsGrantor : Results_Abstractions.IWindPointsGrantor
{
    public JumpScoreModule.WindPoints Grant(Jump jump) => JumpScoreModule.WindPoints.None;
}