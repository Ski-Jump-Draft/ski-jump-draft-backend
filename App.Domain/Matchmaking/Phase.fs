namespace App.Domain.Matchmaking

type PlayersCount = private PlayersCount of int

module PlayersCount =
    type Error =
        | BelowZero of Count: int
        | TooMany of Count: int * Max: int

    let tryCreate (v: int) =
        // if v < 0 then Error(Error.BelowZero v)
        // elif v > 10 then Error(Error.TooMany(v, 10))
        // else Ok(PlayersCount v)
        PlayersCount v

    let value (PlayersCount v) = v

type Phase =
    | Active
    | Ended
    | Failed of Reason: MatchmakingFailReason
