namespace Game.Core.Domain.Shared

[<Struct; StructuralEquality; StructuralComparison>]
type CountryCode = private CountryCode of string  // ISO 3166‑1 α‑2

module CountryCode =
    let tryCreate (s: string) =
        if s.Length = 3 && s.ToUpper() = s then Some (CountryCode s)
        
        else None

    let value (CountryCode s) = s
    