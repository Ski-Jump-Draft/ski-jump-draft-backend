namespace App.Domain.GameWorld

module HillTypes =
    type Id = Id of System.Guid
    
    [<Struct>]
    type KPoint = private KPoint of double

    module KPoint =
        let tryCreate (v: double) =
            if v >= 0.0 then Some(KPoint v) else None

        let value (KPoint v) = v

    [<Struct>]
    type HsPoint = private HsPoint of double

    module HsPoint =
        let tryCreate (v: double) =
            if v >= 0.0 then Some(HsPoint v) else None

        let value (HsPoint v) = v

    module Record =
        type SetterReference =
            | Simple of string
            | GameWorldJumper of JumperId: JumperTypes.Id

        type Distance = private Distance of double

        module Distance =
            type Error = ZeroOrBelow of Value: double

            let tryCreate (v: double) =
                if v > 0 then Ok(Distance v) else Error(ZeroOrBelow v)

            let value (Distance v) = v

    type Record =
        { SetterReference: Record.SetterReference
          Distance: Record.Distance }

    [<Struct>]
    type Name = Name of string

    [<Struct>]
    type Location = Location of string
    
    type Status =
        | Operational
        | Retired

