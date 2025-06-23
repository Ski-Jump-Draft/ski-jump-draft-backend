namespace App.Domain.Profile

open System

module User =
    [<Struct>]
    type Id = Id of System.Guid
    
    type Name = private Name of string
    module Name =
        let tryCreate (v: string) =
            if v.Length >= 3 && v.Length < 25 then
                Ok(Name v)
            else
                Error(invalidOp "Name must be 3-24 characters")
        let value (Name v) = v
    
    module Profile =   
        type Id = private Id of Guid
        module Id =
            let tryCreate (v: Guid) = Id v
            let value (Id v) = v
    
type User =
    {
        Id: User.Id
        ProfileId: User.Profile.Id
        Name: User.Name
    }

