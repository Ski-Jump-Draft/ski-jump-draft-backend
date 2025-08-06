module App.Domain.Game.Event

open App.Domain
open App.Domain.Game.Participant
open App.Domain.Game.Ranking

[<CLIMutable>]
type GameCreatedV1 =
    { GameId: Game.Id.Id
      Settings: Settings.Settings
      Participants: Participants }
//
// [<CLIMutable>]
// type ParticipantJoinedV1 =
//     { GameId: Game.Id.Id
//       ParticipantId: Participant.Id }

[<CLIMutable>]
type ParticipantLeftV1 =
    { GameId: Game.Id.Id
      // ParticipantId: Participant.Id }
      Participant: Participant.Participant }

[<CLIMutable>]
type PreDraftPhaseStartedV1 =
    { GameId: Game.Id.Id
      PreDraftId: PreDraft.Id.Id }

[<CLIMutable>]
type PreDraftPhaseEndedV1 =
    { GameId: Game.Id.Id
      PreDraftId: PreDraft.Id.Id }

[<CLIMutable>]
type DraftPhaseStartedV1 =
    { GameId: Game.Id.Id
      DraftId: Draft.Id.Id }

[<CLIMutable>]
type DraftPhaseEndedV1 =
    { GameId: Game.Id.Id
      DraftId: Draft.Id.Id }

[<CLIMutable>]
type GameCompetitionDto =
    { CompetitionId: App.Domain.SimpleCompetition.CompetitionId }

[<CLIMutable>]
type CompetitionPhaseStartedV1 =
    { GameId: Game.Id.Id
      GameCompetition: GameCompetitionDto }

[<CLIMutable>]
type CompetitionPhaseEndedV1 = { GameId: Game.Id.Id }

[<CLIMutable>]
type GameEndedV1 =
    { GameId: Game.Id.Id
      Ranking: GameRanking }

type GameEventPayload =
    | GameCreatedV1 of GameCreatedV1
    //| ParticipantJoinedV1 of ParticipantJoinedV1
    | ParticipantLeftV1 of ParticipantLeftV1
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
        //| ParticipantJoinedV1 _ -> 1us
        | ParticipantLeftV1 _ -> 1us
        | PreDraftPhaseStartedV1 _ -> 1us
        | PreDraftPhaseEndedV1 _ -> 1us
        | DraftPhaseStartedV1 _ -> 1us
        | DraftPhaseEndedV1 _ -> 1us
        | CompetitionPhaseStartedV1 _ -> 1us
        | CompetitionPhaseEndedV1 _ -> 1us
        | GameEndedV1 _ -> 1us
