namespace App.Domain.Competition

// TODO: Na razie Hill jest Value-Objectem. Może encja? Może AggregateRoot? Może Hill spoza Competition BC?
module Hill =
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

    type Type =
        | Small
        | Medium
        | Normal
        | Large
        | Big
        | SkiFlying

    module Type =
        let fromHS (HsPoint hs) =
            match hs with
            | hs when hs < 50.0 -> Type.Small
            | hs when hs < 85.0 -> Type.Medium
            | hs when hs < 110.0 -> Type.Normal
            | hs when hs < 150.0 -> Type.Large
            | hs when hs < 185 -> Type.Big
            | _ -> Type.SkiFlying

type Hill =
    private
        { KPoint: Hill.KPoint
          HsPoint: Hill.HsPoint }

    static member Create k hs = { KPoint = k; HsPoint = hs }

    member this.Type = Hill.Type.fromHS this.HsPoint

// module Hill =
//     [<Struct>]
//     type Id = Id of System.Guid
//
//     [<Struct>]
//     type KPoint = private KPoint of double
//     module KPoint =
//         let tryCreate (v: double) =
//             if v >= 0.0 then Some(KPoint v) else None
//
//         let value (KPoint v) = v
//
//     [<Struct>]
//     type HSPoint = private HSPoint of double
//     module HSPoint =
//         let tryCreate (v: double) =
//             if v >= 0.0 then Some(HSPoint v) else None
//
//         let value (HSPoint v) = v
//
//     [<Struct>]
//     type PointsForMeter = PointsForMeter of double
//     module PointsForMeter =
//         let tryCreate (v: double) =
//             if v > 0 then Some(PointsForMeter v) else None
//
//         let value (PointsForMeter v) = v
//
//         let fromK (KPoint k) =
//             let points =
//                 match k with
//                 | k when k < 25.0 -> 4.8
//                 | k when k < 30.0 -> 4.4
//                 | k when k < 35.0 -> 4.0
//                 | k when k < 40.0 -> 3.6
//                 | k when k < 50.0 -> 3.2
//                 | k when k < 60.0 -> 2.8
//                 | k when k < 70.0 -> 2.4
//                 | k when k < 80.0 -> 2.2
//                 | k when k < 100.0 -> 2.0
//                 | k when k < 135.0 -> 1.8
//                 | k when k < 180.0 -> 1.6
//                 | _ -> 1.2
//
//             PointsForMeter points
//
//     type Type =
//         | Small
//         | Medium
//         | Normal
//         | Large
//         | Big
//         | SkiFlying
//
//     module Type =
//         let fromHS (HSPoint hs) =
//             match hs with
//             | hs when hs < 50.0 -> Type.Small
//             | hs when hs < 85.0 -> Type.Medium
//             | hs when hs < 110.0 -> Type.Normal
//             | hs when hs < 150.0 -> Type.Large
//             | hs when hs < 185 -> Type.Big
//             | _ -> Type.SkiFlying
//
//
//     [<Struct>]
//     type PointsPerKPoint = PointsPerKPoint of double
//
//     module PointsPerKPoint =
//         let fromHillType =
//             function
//             | SkiFlying -> PointsPerKPoint 120
//             | _ -> PointsPerKPoint 60
//
// open Hill
// type Hill =
//     {
//         Id: Hill.Id
//         KPoint: Hill.KPoint
//         HSPoint: Hill.HSPoint
//     }
//
//     member this.HillType=
//         Hill.Type.fromHS this.HSPoint
//
//     member this.PointsPerMeter =
//         this.KPoint
//         |> PointsForMeter.fromK
//
//
//     member this.PointsPerKPoint =
//         this.HSPoint
//         |> Hill.Type.fromHS
//         |> PointsPerKPoint.fromHillType
