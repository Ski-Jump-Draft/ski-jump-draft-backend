using App.Application.Commanding;
using App.Domain.Competition;
using App.Domain.Competition.Results;
using App.Domain.Competition.Results;
using App.Domain.Competition.Rules;
using App.Plugin.Competitions.StartlistProvider.AdvancementByLimitDecider;
using App.Plugin.Engine.Classic;
using Microsoft.FSharp.Collections;
using static Microsoft.FSharp.Collections.ListModule;
using StartlistModule = App.Domain.Competition.StartlistModule;

namespace App.Plugin.Competitions.StartlistProvider;

public sealed class Classic(
    List<RoundParticipantsLimit> roundLimits,
    IAdvancementByLimitDecider advancementByLimitDecider,
    CompetitionCategory competitionCategory,
    Func<ResultsModule.Results> getResults,
    Func<Phase.RoundIndex> getRoundIndex,
    RankedResults.IRankedResultsFactory rankedResultsFactory,
    // Func<IndividualParticipantModule.HostId, StartlistModule.EntityModule.HostId> mapIndividualParticipantIdToStartlistEntityId,
    // Func<TeamModule.HostId, StartlistModule.EntityModule.HostId> mapTeamIdToStartlistEntityId,
    Func<ParticipantResultModule.Id, IEnumerable<StartlistModule.EntityModule.Id>>
        mapParticipantResultIdToStartlistEntityId
)
    : IStartlistProvider
{
    private readonly HashSet<StartlistModule.EntityModule.Id> _completedInCurrentRound =
        [];

    public FSharpList<StartlistModule.EntityModule.Id> Provide()
    {
        var roundIndex = getRoundIndex();
        var firstStartlistForRound = StartingStartlistPerRound(roundIndex);

        throw new NotImplementedException();
    }

    private FSharpList<StartlistModule.EntityModule.Id> StartingStartlistPerRound(Phase.RoundIndex roundIndex)
    {
        var roundIndexInt = (int)roundIndex.Item;
        var currentLimit = roundLimits[roundIndexInt];
        var results = getResults();
        var rankedResults = rankedResultsFactory.Create(results, roundIndex).Item.ToDictionary();
        if (competitionCategory.IsIndividual)
        {
            // Jeden wynik w konkursie -> jeden start
            var positionById = rankedResults
                .SelectMany(positionAndIds => positionAndIds.Value.Select(id =>
                    {
                        var startlistEntityIds =
                            mapParticipantResultIdToStartlistEntityId(id);
                        return new KeyValuePair<StartlistModule.EntityModule.Id, RankedResults.Position>(
                            startlistEntityIds.Single(),
                            positionAndIds.Key);
                    }
                ))
                .ToDictionary(pair => pair.Key, pair => pair.Value);
            var advanced = advancementByLimitDecider.Decide(positionById, currentLimit);
            return OfSeq(advanced);
        }

        if (competitionCategory.IsTeam)
        {
            throw new NotImplementedException();
        }

        throw new NotImplementedException();
    }


    public void RegisterJump(StartlistModule.EntityModule.Id entityId)
    {
        var roundIndex = getRoundIndex();
        var startingStartlistForRound = StartingStartlistPerRound(roundIndex);
        if (!startingStartlistForRound.Contains(entityId))
        {
            throw new InvalidOperationException(
                $"StartlistProvider.Classic.RegisterJump: entityId ({entityId}) not in startlist");
        }

        if (!_completedInCurrentRound.Add(entityId))
        {
            throw new InvalidOperationException(
                $"StartlistProvider.Classic.RegisterJump: entityId ({entityId}) already registered");
        }
    }
}