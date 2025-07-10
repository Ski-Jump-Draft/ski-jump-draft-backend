namespace App.Domain.GameWorld

module Country =
    [<Struct>]
    type Id = Id of System.Guid
    
    type Code = App.Domain.Shared.CountryCode
    
    // [<Struct; StructuralEquality; StructuralComparison>]
    // type Code = private Code of string  // ISO 3166‑1 α‑2
    //
    // module Code =
    //     let tryCreate (s: string) =
    //         if s.Length = 3 && s.ToUpper() = s then Some (Code s)
    //         
    //         else None
    //
    //     let value (Code s) = s
     
open Country
type Country =
    {
        Id: Country.Id
        Code: Code
    }