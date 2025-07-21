namespace App.Domain.Matchmaking

type PlayersCount = private PlayersCount of int

module PlayersCount =
    type Error =
        | BelowZero of Count: int
        | TooMany of Count: int * Max: int

    let tryCreate (v: int) =
        if v < 0 then Error(Error.BelowZero v)
        elif v > 10 then Error(Error.TooMany(v, 10))
        else Ok(PlayersCount v)
        
    let value (PlayersCount v ) = v

type Phase =
    | Active of Players: Set<Participant.Id>
    | Ended of Players: Set<Participant.Id>
    | Failed of Players: Set<Participant.Id> * Error: MatchmakingFailError

