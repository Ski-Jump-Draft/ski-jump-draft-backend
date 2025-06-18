namespace Game.Core.Domain.Competitions

module RawRules =
    type Type =
        | Individual
        | Team
        
    module Round =
        type TopN = private TopN of int
        module TopN =
            let tryCreate(v: int) =
                if v > 0 && v < 100000000 then Some(TopN v)
                else None
            let value(v: int) = v
        
        type Advancement =
            | TopN of TopN
            | Custom of bool // TODO
            
        type GroupsCount = private GroupsCount of int
        module GroupsCount =
            let tryCreate(v: int) =
                if v > 0 && v < 10000 then Some(GroupsCount v)
                else None
            let value(v: int) = v
            
        type PreferredGroupSize = private PreferredGroupSize of int
        module PreferredGroupSize =
            let tryCreate(v: int) =
                if v > 0 && v < 100 then Some(PreferredGroupSize v)
                else None
            let value(v: int) = v
            
        type Groups =
            | FixedGroupsCount of GroupsCount
            | PreferredGroupSize of PreferredGroupSize
        
    type Round =
        {
            Advancement: Round.Advancement
            Groups: Round.Groups
        }
    
    type Definition = {
        Type: Type
        Rounds: Round list
    }
    
