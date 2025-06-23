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

    type Type =
        | Small
        | Medium
        | Normal
        | Large
        | Big
        | SkiFlying

    module Type =
        let fromHS (HSPoint hs) =
            match hs with
            | hs when hs < 50.0 -> Type.Small
            | hs when hs < 85.0 -> Type.Medium
            | hs when hs < 110.0 -> Type.Normal
            | hs when hs < 150.0 -> Type.Large
            | hs when hs < 185 -> Type.Big
            | _ -> Type.SkiFlying

    [<Struct>]
    type PointsForMeter = PointsForMeter of float

    module PointsForMeter =
        let tryCreate (v: float) =
            if v > 0 then Some(PointsForMeter v) else None

        let value (PointsForMeter v) = v

        let fromK (KPoint k) =
            let points =
                match k with
                | k when k < 25.0 -> 4.8
                | k when k < 30.0 -> 4.4
                | k when k < 35.0 -> 4.0
                | k when k < 40.0 -> 3.6
                | k when k < 50.0 -> 3.2
                | k when k < 60.0 -> 2.8
                | k when k < 70.0 -> 2.4
                | k when k < 80.0 -> 2.2
                | k when k < 100.0 -> 2.0
                | k when k < 135.0 -> 1.8
                | k when k < 180.0 -> 1.6
                | _ -> 1.2

            PointsForMeter points

    [<Struct>]
    type PointsForKPoint = PointsForKPoint of float

    module PointsForKPoint =
        let fromHillType =
            function
            | SkiFlying -> PointsForKPoint 120
            | _ -> PointsForKPoint 60

open Hill
type Hill =
    { Id: Hill.Id
      Location: Location
      Name: Name
      CountryId: Country.Id
      KPoint: KPoint
      HSPoint: HSPoint }

    member this.HillType: Type =
        Type.fromHS this.HSPoint

    member this.PointsPerMeter : PointsForMeter =
        PointsForMeter.fromK this.KPoint

    member this.PointsPerKPoint : PointsForKPoint =
        this.HillType
        |> PointsForKPoint.fromHillType
