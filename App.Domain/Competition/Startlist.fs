namespace App.Domain.Competition

module Startlist =
    type Error = | StartlistEmptyDuringCreation

type Startlist =
    private
        { NextIndividualParticipants: List<IndividualParticipant.Id> }

    static member Create id (nextIndividualParticipants: IndividualParticipant.Id list) =
        Ok { NextIndividualParticipants = nextIndividualParticipants }

    static member Empty id = Ok { NextIndividualParticipants = [] }

    member this.NextParticipant: IndividualParticipant.Id option =
        this.NextIndividualParticipants |> List.tryItem 0

    member this.Contains individualParticipant =
        this.NextIndividualParticipants |> List.contains individualParticipant

    member this.RemoveFirst() =
        { this with
            NextIndividualParticipants = this.NextIndividualParticipants |> List.skip 1 }


type IStartlistProvider =
    abstract Provide: unit -> IndividualParticipant.Id list
    abstract RegisterJump: EntityId: IndividualParticipant.Id -> unit
