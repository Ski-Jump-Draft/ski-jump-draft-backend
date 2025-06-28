namespace App.Domain.GameWorld

module Hill =
    [<Struct>]
    type Id = Id of System.Guid

    [<Struct>]
    type KPoint = private KPoint of float

    module KPoint =
        let tryCreate (v: float) =
            if v >= 0.0 then Some(KPoint v) else None

        let value (KPoint v) = v

    [<Struct>]
    type HSPoint = private HSPoint of float

    module HSPoint =
        let tryCreate (v: float) =
            if v >= 0.0 then Some(HSPoint v) else None

        let value (HSPoint v) = v

    [<Struct>]
    type Name = Name of string

    [<Struct>]
    type Location = Location of string

open Hill

type Hill =
    { Id: Hill.Id
      Location: Location
      Name: Name
      CountryId: Country.Id
      KPoint: KPoint
      HSPoint: HSPoint }
