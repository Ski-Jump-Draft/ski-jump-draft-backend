module App.Domain.Competition.Event

open App
open App.Domain.Competition
open App.Domain.Competition.Results

// type CompetitionStartlistDto =
//     { NextIndividualParticipants: IndividualParticipant.Id list }
//
// // type CompetitionParticipantResultDto = {
// //
// // }
//
// type CompetitionResultsDto =
//     { ParticipantResults: ParticipantResult list } // TODO: Może zrobić "czyste" DTO?

[<CLIMutable>]
type CompetitionEngineConfigDto =
    { EngineName: Engine.Metadata.Name
      EngineVersion: Engine.Metadata.Version
      EngineRawConfig: Map<string, obj>
      GameWorldHillId: Domain.GameWorld.HillTypes.Id
      RandomSeed: uint64 }

[<CLIMutable>]
type CompetitionCreatedV1 =
    { CompetitionId: Id.Id
      EngineConfig: CompetitionEngineConfigDto }

[<CLIMutable>]
type CompetitionStartedV1 = { CompetitionId: Id.Id }

[<CLIMutable>]
type CompetitionRoundStartedV1 =
    { CompetitionId: Id.Id
      RoundIndex: uint }

[<CLIMutable>]
type CompetitionRoundEndedV1 =
    { CompetitionId: Id.Id
      RoundIndex: uint }

[<CLIMutable>]
type CompetitionSuspendedV1 = { CompetitionId: Id.Id }

[<CLIMutable>]
type CompetitionContinuedV1 = { CompetitionId: Id.Id }

[<CLIMutable>]
type CompetitionCancelledV1 = { CompetitionId: Id.Id }

[<CLIMutable>]
type CompetitionEndedV1 = { CompetitionId: Id.Id }

[<CLIMutable>]
type CompetitionJumpRegisteredV1 =
    { CompetitionId: Id.Id
      IndividualParticipantId: IndividualParticipant.Id
      //ParticipantResultId: ParticipantResult.Id
      JumpResultId: JumpResult.Id
      Jump: Jump.Jump }

// [<CLIMutable>]
// type CompetitionEngineSnapshotSavedV1 =
//     { EngineId: Domain.Competition.Engine.Id
//       EngineSnapshot: Domain.Competition.Engine.Snapshot }

type CompetitionEventPayload =
    | CompetitionCreatedV1 of CompetitionCreatedV1
    | CompetitionStartedV1 of CompetitionStartedV1
    | CompetitionRoundStartedV1 of CompetitionRoundStartedV1
    | CompetitionRoundEndedV1 of CompetitionRoundEndedV1
    | CompetitionSuspendedV1 of CompetitionSuspendedV1
    | CompetitionContinuedV1 of CompetitionContinuedV1
    | CompetitionCancelledV1 of CompetitionCancelledV1
    | CompetitionEndedV1 of CompetitionEndedV1
    | CompetitionJumpRegisteredV1 of CompetitionJumpRegisteredV1
    //| CompetitionEngineSnapshotSavedV1 of CompetitionEngineSnapshotSavedV1

module Versioning =
    let schemaVersion =
        function
        | CompetitionCreatedV1 _ -> 1us
        | CompetitionStartedV1 _ -> 1us
        | CompetitionRoundStartedV1 _ -> 1us
        | CompetitionRoundEndedV1 _ -> 1us
        | CompetitionSuspendedV1 _ -> 1us
        | CompetitionContinuedV1 _ -> 1us
        | CompetitionCancelledV1 _ -> 1us
        | CompetitionEndedV1 _ -> 1us
        | CompetitionJumpRegisteredV1 _ -> 1us
        //| CompetitionEngineSnapshotSavedV1 _ -> 1us
