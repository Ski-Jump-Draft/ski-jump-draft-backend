module App.Domain.Game.Id

type Id = Id of System.Guid
module Id =
    let value (Id v) = v