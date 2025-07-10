namespace App.Domain.GameWorld

module Jumper =
    [<Struct>]
    type Id = Id of System.Guid
    
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
    { Id: Jumper.Id
      Name: Jumper.Name
      Surname: Jumper.Surname
      CountryId: Country.Id
    }
