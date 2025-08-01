module App.Domain.Competition.Engine

open App.Domain.Competition.Phase

type IOptions = interface end

type Snapshot = Snapshot of byte[]

type IRng<'State> =
    abstract NextUInt64: unit -> uint64
    abstract State: 'State
    abstract WithState: 'State -> IRng<'State>

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
    type Version = private Version of string

    module Version =
        let create (s: string) =
            let parts = s.Split('.')

            if
                parts.Length = 3
                && parts |> Array.forall (fun p -> System.Int32.TryParse(p) |> fst)
            then
                Version s
            else
                invalidArg "s" $"Invalid version format: {s}"

        let value (Version v) = v

        let major (Version v) = v.Split('.') |> Array.head |> int

        let minor (Version v) = v.Split('.') |> Array.item 1 |> int

        let patch (Version v) = v.Split('.') |> Array.item 2 |> int

        let ofParts major minor patch = $"{major}.{minor}.{patch}" |> create

type IMetadata =
    abstract Name: Metadata.Name
    abstract Description: Metadata.Description
    abstract Author: Metadata.Author

type Event =
    | JumpRegistered of IndividualParticipantId: IndividualParticipant.Id * JumpResultId: Results.JumpResult.Id
    | ParticipantDisqualified
    | RoundStarted of Index: uint
    | RoundEnded of Index: uint
    | CompetitionEnded

type IEngine =
    abstract Id: Id
    abstract Category: Rules.CompetitionCategory
    /// Starts and ends the rounds
    abstract RegisterJump: Jump.Jump -> Snapshot * Event list

    abstract ShouldEndCompetition: bool
    abstract Phase: Phase
    abstract GenerateResults: unit -> ResultsModule.Results
    abstract GenerateStartlist: unit -> Startlist
    abstract ToSnapshot: unit -> Snapshot
    abstract LoadSnapshot: Snapshot -> unit
