namespace Game.Core.Domain

open Ids

[<Struct>]
type JumperName = private JumperName of string

module JumperName =
    let tryCreate (s: string) =
        let trimmed = s.Trim()

        if trimmed.Length > 0 then
            Some(JumperName trimmed)
        else
            None

    let value (JumperName n) = n

[<Struct>]
type JumperSurname = private JumperSurname of string

module JumperSurname =
    let tryCreate (s: string) =
        let trimmed = s.Trim()

        if trimmed.Length > 0 then
            Some(JumperSurname trimmed)
        else
            None

    let value (JumperSurname n) = n
    

type Jumper =
    { Id: JumperId
      Name: JumperName
      Surname: JumperSurname
      JumperSkills: JumperSkills
    }
