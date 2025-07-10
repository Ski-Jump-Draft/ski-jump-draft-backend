namespace App.Domain.Competition.Rules

// TODO: Prefix competitions

module Shared =
    type TeamSize = private TeamSize of uint

    module TeamSize =
        let tryCreate (v: uint) =
            if v < 10u then Some(TeamSize v) else None

        let value (TeamSize v) = v


type CompetitionCategory =
    | Individual
    | Team
    | Mixed

module ComeptitionCategory =
    let tryParse (str: string) =
        match str with
        | "Individual" -> Some Individual
        | "Team" -> Some Team
        | "Mixed" -> Some Mixed
        | _ -> None
