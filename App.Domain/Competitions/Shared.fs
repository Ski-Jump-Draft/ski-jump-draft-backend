namespace App.Domain.Competitions.Rules

module Shared =
    type TeamSize = private TeamSize of uint

    module TeamSize =
        let tryCreate (v: uint) =
            if v < 10u then Some(TeamSize v) else None

        let value (TeamSize v) = v