namespace App.Domain.Matchmaking

type MatchmakingFailReason =
    | NotEnoughPlayers of Count: int * Minimum: int
    | InternalError of Details: obj
