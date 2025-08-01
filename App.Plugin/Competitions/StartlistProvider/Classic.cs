using App.Application.Commanding;
using App.Domain.Competition;
using App.Domain.Competition.Results;
using App.Domain.Competition.Results;
using App.Domain.Competition.Rules;
using App.Plugin.Competitions.StartlistProvider.AdvancementByLimitDecider;
using App.Plugin.Engine.Classic;
using Microsoft.FSharp.Collections;
using static Microsoft.FSharp.Collections.ListModule;
using IndividualParticipantModule = App.Domain.Competition.IndividualParticipantModule;
using ParticipantResultModule = App.Domain.Competition.Results.ParticipantResultModule;
using StartlistModule = App.Domain.Competition.StartlistModule;

namespace App.Plugin.Competitions.StartlistProvider;

public sealed class Classic(
    List<RoundParticipantsLimit> roundLimits,
    IAdvancementByLimitDecider advancementByLimitDecider,
    CompetitionCategory competitionCategory,
    Func<FSharpList<ParticipantResult>> getParticipantResults,
    Func<Phase.RoundIndex> getRoundIndex,
    RankedResults.IRankedResultsFactory rankedResultsFactory,
    Func<ParticipantResultModule.Id, IEnumerable<IndividualParticipantModule.Id>>
        mapParticipantResultIdToIndividualParticipantId
)
    : IStartlistProvider
{
    private readonly HashSet<IndividualParticipantModule.Id> _completedInCurrentRound =
        [];

    public FSharpList<IndividualParticipantModule.Id> Provide()
    {
        var roundIndex = getRoundIndex();
        var firstStartlistForRound = StartingStartlistPerRound(roundIndex);

        // TODO: Wpisać tu TODO (meta-TODO!)

        throw new NotImplementedException();
    }

    private FSharpList<IndividualParticipantModule.Id> StartingStartlistPerRound(Phase.RoundIndex roundIndex)
    {
        var roundIndexInt = (int)roundIndex.Item;
        var currentLimit = roundLimits[roundIndexInt];
        var participantResults = getParticipantResults();
        var rankedResults = rankedResultsFactory.Create(participantResults, roundIndex).Item.ToDictionary();
        if (competitionCategory.IsIndividual)
        {
            var positionByIdIndividualParticipantId = rankedResults
                .SelectMany(positionAndIds => positionAndIds.Value.Select(participantResultId =>
                    {
                        var participantResult = participantResults.ToList().Find(r => r.Id.Equals(participantResultId));
                        if (participantResult is null)
                        {
                            throw new InvalidOperationException("Inconsistency in results");
                        }

                        var individualResultDetails =
                            ((participantResult.Details) as ParticipantResultModule.Details.IndividualResultDetails)!
                            .Item;
                        var individualParticipantId = individualResultDetails.IndividualParticipantId;

                        return new KeyValuePair<IndividualParticipantModule.Id, RankedResults.Position>(
                            individualParticipantId,
                            positionAndIds.Key);
                    }
                ))
                .ToDictionary(pair => pair.Key, pair => pair.Value);
            
            var advanced = advancementByLimitDecider.Decide(positionByIdIndividualParticipantId, currentLimit);
            return OfSeq(advanced);
        }

        if (competitionCategory.IsTeam)
        {
            throw new NotImplementedException();
        }

        throw new NotImplementedException();
    }


    public void RegisterJump(IndividualParticipantModule.Id individualParticipantId)
    {
        var roundIndex = getRoundIndex();
        var startingStartlistForRound = StartingStartlistPerRound(roundIndex);
        if (!startingStartlistForRound.Contains(individualParticipantId))
        {
            throw new InvalidOperationException(
                $"StartlistProvider.Classic.RegisterJump: entityId ({individualParticipantId}) not in startlist");
        }

        if (!_completedInCurrentRound.Add(individualParticipantId))
        {
            throw new InvalidOperationException(
                $"StartlistProvider.Classic.RegisterJump: entityId ({individualParticipantId}) already registered");
        }
    }
}