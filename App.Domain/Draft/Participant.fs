module App.Domain.Draft.Participant

open System

// TODO: Osobny participant dla draft/competition

[<Struct>]
type Id = Id of System.Guid

module Id =
    let tryCreate (v: Guid) = Id v
    let value (Id v) = v

type Name = private Name of string

module Name =
    let tryCreate (v: string) : Result<Name, string> =
        if v.Length >= 3 && v.Length < 25 then
            Ok(Name v)
        else
            Error("Name must be 3-24 characters")

    let value (Name v) = v

type Participant =
    { Id: Id } //; Name: Name }
