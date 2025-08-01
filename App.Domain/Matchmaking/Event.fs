module App.Domain.Matchmaking.Event

open App.Domain.Matchmaking

[<CLIMutable>]
type MatchmakingCreatedV1 =
    { MatchmakingId: Id
      Settings: Settings }

[<CLIMutable>]
type MatchmakingFailedV1 =
    { MatchmakingId: Id
      Reason: MatchmakingFailReason
      ParticipantsCount: int }

[<CLIMutable>]
type MatchmakingEndedV1 =
    { MatchmakingId: Id
      ParticipantsCount: int }

type MatchmakingParticipantDtoV1 =
    { Id: Participant.Id
      Nick: Participant.Nick }

[<CLIMutable>]
type MatchmakingParticipantJoinedV1 =
    { MatchmakingId: Id
      Participant: MatchmakingParticipantDtoV1 }

[<CLIMutable>]
type MatchmakingParticipantLeftV1 =
    { MatchmakingId: Id
      ParticipantId: Participant.Id }

type MatchmakingEventPayload =
    | MatchmakingCreatedV1 of MatchmakingCreatedV1
    | MatchmakingFailedV1 of MatchmakingFailedV1
    | MatchmakingEndedV1 of MatchmakingEndedV1
    | MatchmakingParticipantJoinedV1 of MatchmakingParticipantJoinedV1
    | MatchmakingParticipantLeftV1 of MatchmakingParticipantLeftV1

module Versioning =
    let schemaVersion =
        function
        | MatchmakingCreatedV1 _ -> 1us
        | MatchmakingFailedV1 _ -> 1us
        | MatchmakingEndedV1 _ -> 1us
        | MatchmakingParticipantJoinedV1 _ -> 1us
        | MatchmakingParticipantLeftV1 _ -> 1us
