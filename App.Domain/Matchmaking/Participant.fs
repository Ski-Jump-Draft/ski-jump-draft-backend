namespace App.Domain.Matchmaking

module Participant =
    type Id = Id of System.Guid

type Participant = {
    Id: Participant.Id
}