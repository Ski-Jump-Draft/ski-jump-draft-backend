module App.Domain.Game.Event

open App.Domain
open App.Domain.Game.Hosting
open App.Domain.Game.Ranking

[<Struct; CLIMutable>]
type GameCreatedV1 =
    { GameId: Game.Id.Id
      HostId: Host.Id
      Settings: Settings.Settings }

[<Struct; CLIMutable>]
type ParticipantJoinedV1 =
    { GameId: Game.Id.Id
      ParticipantId: Participant.Id }

[<Struct; CLIMutable>]
type ParticipantLeftV1 =
    { GameId: Game.Id.Id
      ParticipantId: Participant.Id }

[<Struct; CLIMutable>]
type MatchmakingPhaseStartedV1 = { GameId: Game.Id.Id }

[<Struct; CLIMutable>]
type MatchmakingPhaseEndedV1 = { GameId: Game.Id.Id }

[<Struct; CLIMutable>]
type PreDraftPhaseStartedV1 =
    { GameId: Game.Id.Id
      PreDraftId: PreDraft.Id.Id }

[<Struct; CLIMutable>]
type PreDraftPhaseEndedV1 =
    { GameId: Game.Id.Id
      PreDraftId: PreDraft.Id.Id }

[<Struct; CLIMutable>]
type DraftPhaseStartedV1 =
    { GameId: Game.Id.Id
      DraftId: Draft.Id.Id }

[<Struct; CLIMutable>]
type DraftPhaseEndedV1 =
    { GameId: Game.Id.Id
      DraftId: Draft.Id.Id }

[<Struct; CLIMutable>]
type CompetitionPhaseStartedV1 =
    { GameId: Game.Id.Id
      CompetitionId: Game.Competition.Id }

[<Struct; CLIMutable>]
type CompetitionPhaseEndedV1 =
    { GameId: Game.Id.Id
      CompetitionId: Game.Competition.Id }

[<Struct; CLIMutable>]
type GameEndedV1 =
    { GameId: Game.Id.Id
      Results: EndedGameResults.Ranking }

type GameEventPayload =
    | GameCreatedV1 of GameCreatedV1
    | ParticipantJoinedV1 of ParticipantJoinedV1
    | ParticipantLeftV1 of ParticipantLeftV1
    | MatchmakingPhaseStartedV1 of MatchmakingPhaseStartedV1
    | MatchmakingPhaseEndedV1 of MatchmakingPhaseEndedV1
    | PreDraftPhaseStartedV1 of PreDraftPhaseStartedV1
    | PreDraftPhaseEndedV1 of PreDraftPhaseEndedV1
    | DraftPhaseStartedV1 of DraftPhaseStartedV1
    | DraftPhaseEndedV1 of DraftPhaseEndedV1
    | CompetitionPhaseStartedV1 of CompetitionPhaseStartedV1
    | CompetitionPhaseEndedV1 of CompetitionPhaseEndedV1
    | GameEndedV1 of GameEndedV1

module Versioning =
    let schemaVersion =
        function
        | GameCreatedV1 _ -> 1us
        | ParticipantJoinedV1 _ -> 1us
        | ParticipantLeftV1 _ -> 1us
        | MatchmakingPhaseStartedV1 _ -> 1us
        | MatchmakingPhaseEndedV1 _ -> 1us
        | PreDraftPhaseStartedV1 _ -> 1us
        | PreDraftPhaseEndedV1 _ -> 1us
        | DraftPhaseStartedV1 _ -> 1us
        | DraftPhaseEndedV1 _ -> 1us
        | CompetitionPhaseStartedV1 _ -> 1us
        | CompetitionPhaseEndedV1 _ -> 1us
        | GameEndedV1 _ -> 1us
