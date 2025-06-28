module App.Domain.Draft.Id

type Id = Id of System.Guid
module Id =
    let value (Id v) = v