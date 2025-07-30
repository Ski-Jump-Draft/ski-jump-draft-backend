using App.Domain.Competition;
using App.Domain.Competition.Results;
using App.Domain.Competition.Results;
using App.Plugin.Engine.Classic;

namespace App.Plugin.Competitions.StartlistProvider.AdvancementByLimitDecider;

public interface IAdvancementByLimitDecider
{
    IEnumerable<StartlistModule.EntityModule.Id> Decide(
        Dictionary<StartlistModule.EntityModule.Id, RankedResults.Position> position, RoundParticipantsLimit limit);
}