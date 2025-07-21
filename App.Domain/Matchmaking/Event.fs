module App.Domain.Matchmaking.Event

open App.Domain.Matchmaking

[<Struct; CLIMutable>]
type MatchmakingCreatedV1 =
    { MatchmakingId: Id
      Settings: Settings }

[<Struct; CLIMutable>]
type MatchmakingFailedV1 =
    { MatchmakingId: Id
      Error: MatchmakingFailError; PlayersCount: int }

[<Struct; CLIMutable>]
type MatchmakingEndedV1 =
    { MatchmakingId: Id; PlayersCount: int }

[<Struct; CLIMutable>]
type MatchmakingPlayerJoinedV1 =
    { MatchmakingId: Id
      ParticipantId: Participant.Id }

[<Struct; CLIMutable>]
type MatchmakingPlayerLeftV1 =
    { MatchmakingId: Id
      ParticipantId: Participant.Id }

type MatchmakingEventPayload =
    | MatchmakingCreatedV1 of MatchmakingCreatedV1
    | MatchmakingFailedV1 of MatchmakingFailedV1
    | MatchmakingEndedV1 of MatchmakingEndedV1
    | MatchmakingPlayerJoinedV1 of MatchmakingPlayerJoinedV1
    | MatchmakingPlayerLeftV1 of MatchmakingPlayerLeftV1

module Versioning =
    let schemaVersion =
        function
        | MatchmakingCreatedV1 _ -> 1us
        | MatchmakingFailedV1 _ -> 1us
        | MatchmakingEndedV1 _ -> 1us
        | MatchmakingPlayerJoinedV1 _ -> 1us
        | MatchmakingPlayerLeftV1 _ -> 1us
