namespace App.Domain.GameWorld

open App.Domain.Shared.Ids

module Country =
    [<Struct; StructuralEquality; StructuralComparison>]
    type Code = private Code of string  // ISO 3166‑1 α‑2

    module Code =
        let tryCreate (s: string) =
            if s.Length = 3 && s.ToUpper() = s then Some (Code s)
            
            else None

        let value (Code s) = s
     
open Country
type Country =
    {
        Id: CountryId
        Code: Code
    }