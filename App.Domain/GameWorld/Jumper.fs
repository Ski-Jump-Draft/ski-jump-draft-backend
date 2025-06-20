namespace App.Domain.GameWorld

open App.Domain
open App.Domain.Shared.Ids

module Jumper =
    [<Struct>]
    type Name = private Name of string

    module Name =
        let tryCreate (s: string) =
            let trimmed = s.Trim()

            if trimmed.Length > 0 then
                Some(Name trimmed)
            else
                None

        let value (Name n) = n

    [<Struct>]
    type Surname = private Surname of string

    module Surname =
        let tryCreate (s: string) =
            let trimmed = s.Trim()

            if trimmed.Length > 0 then
                Some(Surname trimmed)
            else
                None

        let value (Surname n) = n
        

type Jumper =
    { Id: JumperId
      Name: Jumper.Name
      Surname: Jumper.Surname
      CountryId: CountryId
    }
