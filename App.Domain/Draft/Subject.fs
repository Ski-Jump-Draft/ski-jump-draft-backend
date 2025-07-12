module App.Domain.Draft.Subject

// TODO: Normalne errory. W game world też. W country code też.

[<Struct>]
type Id = Id of System.Guid

type CountryCode = App.Domain.Shared.CountryCode

module Jumper =
    type Name = Name of string
    type Surname = Surname of string
    
    // [<Struct>]
    // type Name = private Name of string
    //
    // module Name =
    //     let tryCreate (s: string) =
    //         let trimmed = s.Trim()
    //
    //         if trimmed.Length > 0 then Some(Name trimmed) else None
    //
    //     let value (Name n) = n
    //
    // [<Struct>]
    // type Surname = private Surname of string
    //
    // module Surname =
    //     let tryCreate (s: string) =
    //         let trimmed = s.Trim()
    //
    //         if trimmed.Length > 0 then Some(Surname trimmed) else None
    //
    //     let value (Surname n) = n



type Jumper =
    { Name: Jumper.Name
      Surname: Jumper.Surname
      CountryCode: CountryCode }

module Team =
    [<Struct>]
    type Name = private Name of string

    module Name =
        let tryCreate (s: string) =
            let trimmed = s.Trim()

            if trimmed.Length > 0 && trimmed.Length < 50 then
                Some(Name trimmed)
            else
                None

        let value (Name n) = n

    type CountryCode = App.Domain.Shared.CountryCode

type Team =
    { Name: Team.Name
      CountryCode: CountryCode }

type Identity =
    | Jumper of Jumper
    | Team of Team

type Subject = { Id: Id; Identity: Identity }
