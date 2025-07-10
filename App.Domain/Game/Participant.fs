module App.Domain.Game.Participant

type Id = Id of System.Guid

type Participant = {
    Id: Id
}