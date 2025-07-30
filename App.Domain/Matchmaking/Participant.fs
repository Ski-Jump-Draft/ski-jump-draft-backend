namespace App.Domain.Matchmaking

module Participant =
    type Id = Id of System.Guid
    type Nick = private Nick of string

    module Nick =
        type Error = InvalidLength of Min: int * Max: int

        let tryCreate (v: string) =
            if v.Length > 25 then
                Error(Error.InvalidLength(3, 25))
            else
                Ok(Nick v)

        let value (Nick v) = v



type Participant =
    { Id: Participant.Id
      Nick: Participant.Nick }
