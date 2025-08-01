using App.Util;
using App.Domain.Competition;
using App.Domain.Competition.Results;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using ParticipantResultModule = App.Domain.Competition.Results.ParticipantResultModule;

namespace App.Plugin.Competitions.RankedResultsCreator;

public class Default(RankedResults.ExAequoPolicy exAequoPolicy)
    : RankedResults.IRankedResultsFactory
{
    public RankedResults.RankedResults Create(FSharpList<ParticipantResult> participantResults,
        FSharpOption<Phase.RoundIndex> roundIndex)
    {
        var results = ResultsModule.Results.FromState(participantResults).ResultValue;
        var totalPointsById = results.MapTotalPointsByRound(roundIndex);

        var sorted = totalPointsById
            .OrderByDescending(pair => pair.Value.Item)
            .ToList();

        var ranked = ApplyExAequoPolicy(sorted);

        var fsharpRanked = FSharpInterop.ToFSharpMap(ranked);

        return RankedResults.RankedResults.NewRankedResults(fsharpRanked);
    }

    private Dictionary<RankedResults.Position, List<ParticipantResultModule.Id>> ApplyExAequoPolicy(
        List<KeyValuePair<ParticipantResultModule.Id, ParticipantResultModule.TotalPoints>> sorted)
    {
        var result = new Dictionary<RankedResults.Position, List<ParticipantResultModule.Id>>();
        var currentRank = 1;
        var index = 0;

        while (index < sorted.Count)
        {
            var group = new List<KeyValuePair<ParticipantResultModule.Id, ParticipantResultModule.TotalPoints>>();
            var currentPoints = sorted[index].Value;

            while (index + group.Count < sorted.Count &&
                   sorted[index + group.Count].Value.Equals(currentPoints))
            {
                group.Add(sorted[index + group.Count]);
            }

            if (exAequoPolicy.IsNone)
            {
                for (int i = 0; i < group.Count; i++)
                {
                    var position = RankedResults.PositionModule.tryCreate(currentRank + i).ResultValue;
                    result[position] = new List<ParticipantResultModule.Id> { group[i].Key };
                }

                currentRank += group.Count;
            }
            else
            {
                var position = RankedResults.PositionModule.tryCreate(currentRank).ResultValue;
                var ids = group.Select(kvp => kvp.Key).ToList();
                result[position] = ids;

                currentRank += exAequoPolicy.IsAddOne ? 1 : group.Count;
            }

            index += group.Count;
        }

        return result;
    }
}