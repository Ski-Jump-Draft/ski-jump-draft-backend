namespace App.Domain.Matchmaking

open App.Domain.Matchmaking

type Error =
    | InvalidPhase of Expected: PhaseTag list * Actual: PhaseTag
    | PlayerAlreadyJoined of PlayerId: Participant.Id
    | RoomFull of Count: int
    | PlayerNotJoined of PlayerId: Participant.Id
    | InternalError

