namespace App.Domain.Shared

[<Struct; StructuralEquality; StructuralComparison>]
type CountryCode = private CountryCode of string

module CountryCode =
    let tryCreate (s: string) =
        if s.Length = 3 && s.ToUpper() = s then Some (CountryCode s)
        
        else None

    let value (CountryCode s) = s