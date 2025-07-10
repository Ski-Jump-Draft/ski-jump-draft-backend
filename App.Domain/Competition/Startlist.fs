namespace App.Domain.Competition

open App.Domain.Competition.Results.ResultObjects

module Startlist =
    type Id = Id of System.Guid

    type Error = | StartlistEmptyDuringCreation

    module EntityModule =
        type Id = Id of System.Guid

    type Entity = { Id: EntityModule.Id }

type Startlist =
    private
        { Id: Startlist.Id
          NextEntities: List<Startlist.Entity> }

    static member Create id (nextEntities: Startlist.Entity list) =
        Ok { Id = id; NextEntities = nextEntities }

    static member Empty id = Ok { Id = id; NextEntities = [] }

    member this.Next: Startlist.Entity option = this.NextEntities |> List.tryItem 0

    member this.Contains entity =
        this.NextEntities |> List.contains entity

    member this.RemoveFirst() =
        { this with
            NextEntities = this.NextEntities |> List.skip 1 }


type IStartlistProvider =
    // abstract Provide: RoundIndex: Phase.RoundIndex -> Startlist.EntityModule.Id list
    abstract Provide: unit -> Startlist.EntityModule.Id list
    abstract RegisterJump: EntityId: Startlist.EntityModule.Id -> unit
// abstract Provide:
//     RoundIndex: Phase.RoundIndex *
//     Results: ParticipantResult list *
//     MapToStartlistEntity: (ParticipantResult -> Startlist.Entity.Id) ->
//         Startlist.Entity.Id list
