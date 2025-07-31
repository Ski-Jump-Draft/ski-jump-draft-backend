namespace App.Domain.Matchmaking

open App.Domain.Matchmaking

type Error =
    | InvalidPhase of Expected: PhaseTag list * Actual: PhaseTag
    | ParticipantAlreadyJoined of ParticipantId: Participant.Id
    | RoomFull of Count: int
    | ParticipantNotInMatchmaking of ParticipantId: Participant.Id
    | InternalError
