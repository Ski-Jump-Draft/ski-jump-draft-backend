namespace App.Domain.Competitions.Rules

open App.Domain.CustomStrategies

/// TODO: Raw rules chyba trzeba przemyśleć
module Raw =
    type Type =
        | Individual
        | Team

    module Round =
        type TopN = private TopN of int

        module TopN =
            let tryCreate (v: int) =
                if v > 0 && v < 100000000 then Some(TopN v) else None

            let value (v: int) = v

        type Advancement =
            | TopN of TopN
            | Custom of CustomStrategy.Ref

        type GroupsCount = private GroupsCount of int

        module GroupsCount =
            let tryCreate (v: int) =
                if v > 0 && v < 10000 then Some(GroupsCount v) else None

            let value (v: int) = v

        type PreferredGroupSize = private PreferredGroupSize of int

        module PreferredGroupSize =
            let tryCreate (v: int) =
                if v > 0 && v < 100 then
                    Some(PreferredGroupSize v)
                else
                    None

            let value (v: int) = v

        type Groups =
            | FixedGroupsCount of GroupsCount
            | PreferredGroupSize of PreferredGroupSize

    type Round =
        { Advancement: Round.Advancement
          Groups: Round.Groups }

open Raw
type Raw = { Type: Type; Rounds: Round list }
