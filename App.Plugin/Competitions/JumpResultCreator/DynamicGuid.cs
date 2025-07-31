using App.Application.Commanding;
using App.Domain.Competition;
using App.Domain.Competition.Results;
using App.Domain.Competition.Results;
using App.Domain.Shared;

namespace App.Plugin.Competitions.JumpResultCreator;

public class DynamicGuid(
    IGuid guid) : Abstractions.IJumpResultCreator
{
    public JumpResult Create(JumpScore score, Phase.RoundIndex roundIndex, IndividualParticipantModule.Id individualParticipantId)
    {
        var id = guid.NewGuid();
        // var roundIndex = roundIndexProvider.Provide();
        // var individualParticipantId = individualParticipantIdProvider.Provide();

        return new JumpResult(
            id: JumpResultModule.Id.NewId(id),
            individualParticipantId: individualParticipantId,
            roundIndex: roundIndex,
            score: score
        );
    }
}