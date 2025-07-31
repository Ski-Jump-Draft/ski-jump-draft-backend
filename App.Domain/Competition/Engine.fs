module App.Domain.Competition.Engine

open App.Domain.Competition.Phase

type IOptions = interface end

/// blob
type EngineSnapshotBlob = EngineSnapshotBlob of byte[]

type Phase =
    | Ended
    | Running of RoundIndex: RoundIndex
    | WaitingForNextRound of NextRoundIndex: RoundIndex

type Id = Id of System.Guid

module Metadata =
    type Id = Id of System.Guid
    type Name = Name of string
    type Description = Description of string
    type Author = Author of string

type Metadata =
    abstract Name: Metadata.Name
    abstract Description: Metadata.Description
    abstract Author: Metadata.Author

// type IEngine =
//     abstract Id: Id
//     abstract Category: Rules.CompetitionCategory
//
//     /// Starts and ends the rounds
//     abstract RegisterJump: Jump.Jump -> EngineSnapshotBlob
//
//     // TODO: Do not invoke programatically outside IEngine implementation
//     abstract SetUpNextRound: unit -> EngineSnapshotBlob option
//
//     // TODO: Do not invoke programatically outside IEngine implementation
//     abstract EndRound: unit -> EngineSnapshotBlob option
//
//     abstract ShouldEndCompetition: bool
//
//     abstract Phase: Phase
//     abstract ResultsState: ResultsModule.ResultsState
//     abstract Startlist: Startlist
//     abstract ToSnapshot: unit -> EngineSnapshotBlob
//     abstract LoadSnapshot: EngineSnapshotBlob -> unit

type Event =
    | JumpRegistered of ParticipantResultId: Results.ParticipantResult.Id * JumpResultId: Results.JumpResult.Id
    | ParticipantDisqualified
    | RoundStarted of Index: uint
    | RoundEnded of Index: uint
    | CompetitionEnded

type IEngine =
    abstract Id: Id
    abstract InitialCategory: Rules.CompetitionCategory

    /// Starts and ends the rounds
    abstract RegisterJump: Jump.Jump -> EngineSnapshotBlob * Event list

    abstract ShouldEndCompetition: bool
    abstract Phase: Phase
    abstract GenerateResults: unit -> ResultsModule.Results
    abstract GenerateStartlist: unit -> Startlist
    abstract ToSnapshot: unit -> EngineSnapshotBlob
    abstract LoadSnapshot: EngineSnapshotBlob -> unit
