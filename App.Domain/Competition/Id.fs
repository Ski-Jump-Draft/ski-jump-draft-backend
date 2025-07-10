module App.Domain.Competition.Id

type Id = Id of System.Guid
module Id =
    let value (Id v) = v