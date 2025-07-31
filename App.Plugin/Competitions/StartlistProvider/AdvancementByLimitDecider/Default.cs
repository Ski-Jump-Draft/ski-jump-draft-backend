using App.Application.Commanding;
using App.Domain.Competition;
using App.Domain.Competition.Results;
using App.Plugin.Engine.Classic;

namespace App.Plugin.Competitions.StartlistProvider.AdvancementByLimitDecider;

public class Default(IAdvancementTieBreaker advancementTieBreaker) : IAdvancementByLimitDecider
{
    public IEnumerable<StartlistModule.EntityModule.Id> Decide(
        Dictionary<StartlistModule.EntityModule.Id, RankedResults.Position> positionsByEntity,
        RoundParticipantsLimit limit)
    {
        return limit switch
        {
            RoundParticipantsLimit.Soft softLimit => positionsByEntity
                .Where(entityAndPosition =>
                    RankedResults.PositionModule.value(entityAndPosition.Value) <= softLimit.Limit)
                .Select(entityAndPosition => entityAndPosition.Key),

            RoundParticipantsLimit.Exact exactLimit =>
                positionsByEntity
                    .Where(entityAndPosition =>
                        RankedResults.PositionModule.value(entityAndPosition.Value) < exactLimit.Limit)
                    .Select(entityAndPosition => entityAndPosition.Key)
                    .Concat(
                        advancementTieBreaker
                            .BreakTies(
                                positionsByEntity
                                    .Where(entityAndPosition =>
                                        RankedResults.PositionModule.value(entityAndPosition.Value) == exactLimit.Limit)
                                    .Select(entityAndPosition => entityAndPosition.Key.Item)
                            )
                            .Select(StartlistModule.EntityModule.Id.NewId)
                    ),

            RoundParticipantsLimit.None => positionsByEntity.Select(entityAndPosition => entityAndPosition.Key),

            _ => throw new ArgumentOutOfRangeException()
        };
    }
}