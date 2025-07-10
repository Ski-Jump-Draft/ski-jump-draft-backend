using App.Domain.Competition;

namespace App.Plugin.Competitions.NextRoundStartDecider;

public class ExactRoundsLimit(int lastRoundIndex) : Advancement.INextRoundStartDecider
{
    public Advancement.NextRoundStartDecision Decide(Phase.RoundIndex endedRoundIndex)
    {
        var endedRoundIndexInt = (int)endedRoundIndex.Item;
        return endedRoundIndexInt == lastRoundIndex
            ? new Advancement.NextRoundStartDecision(false)
            : new Advancement.NextRoundStartDecision(true);
    }
}