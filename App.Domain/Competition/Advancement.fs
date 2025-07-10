module App.Domain.Competition.Advancement

open App.Domain.Competition.Phase

type NextRoundStartDecision =
    {
        StartNextRound: bool
    }
    
type INextRoundStartDecider =
    abstract member Decide: EndedRoundIndex: RoundIndex -> NextRoundStartDecision