namespace App.Domain.Player

open App.Domain.Shared.Ids

module Player =
    type Name = private Name of string
    module Name =
        let tryCreate (s: string) =
            if s.Length >= 3 && s.Length <= 30 && s = s.Trim() then
                Some (Name s)
            else
                None

        let value (Name s) = s
        
open Player
type Player = {
    Id: PlayerId
    Name: Name
}