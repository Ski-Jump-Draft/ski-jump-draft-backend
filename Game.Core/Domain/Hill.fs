namespace Game.Core.Domain

open Ids

[<Struct>]
type KPoint = private KPoint of float

module KPoint =
    let tryCreate (v: float) =
        if v >= 0.0 then Some (KPoint v) else None

    let value (KPoint v) = v

[<Struct>]
type HSPoint = private HSPoint of float

module HSPoint =
    let tryCreate (v: float) =
        if v >= 0.0 then Some (HSPoint v) else None

    let value (HSPoint v) = v
    
[<Struct>]
type HillName = HillName of string

[<Struct>]
type HillLocation = HillLocation of string

type HillType =
    | Small
    | Medium
    | Normal
    | Large
    | Big
    | SkiFlying
    
module HillType =
    let fromHS(HSPoint hs) =
        match hs with
        | hs when hs < 50.0 -> HillType.Small
        | hs when hs < 85.0 -> HillType.Medium
        | hs when hs < 110.0 -> HillType.Normal
        | hs when hs < 150.0 -> HillType.Large
        | hs when hs < 185 -> HillType.Big
        | _ -> HillType.SkiFlying
        
[<Struct>]
type HillPointsForMeter = HillPointsForMeter of float

module HillPointsForMeter =
    let tryCreate(v: float) =
        if v > 0 then Some (HillPointsForMeter v) else None
    let value (HillPointsForMeter v) = v
    let fromK(KPoint k) =
        let points =
            match k with
            | k when k < 25.0  -> 4.8
            | k when k < 30.0  -> 4.4
            | k when k < 35.0  -> 4.0
            | k when k < 40.0  -> 3.6
            | k when k < 50.0  -> 3.2
            | k when k < 60.0  -> 2.8
            | k when k < 70.0  -> 2.4
            | k when k < 80.0 -> 2.2
            | k when k < 100.0 -> 2.0
            | k when k < 135.0 -> 1.8
            | k when k < 180.0 -> 1.6
            | _                -> 1.2
        HillPointsForMeter points

[<Struct>]
type HillPointsForKPoint = HillPointsForKPoint of float

module HillPointsForKPoint =
    let fromHillType = function
            | SkiFlying -> HillPointsForKPoint 120
            | _ -> HillPointsForKPoint 60

type Hill =
    { Id: HillId
      Location: HillLocation
      Name: HillName
      CountryCode: CountryCode
      KPoint: KPoint
      HSPoint: HSPoint
      SimulationSettings: HillSimulationSettings }
    
module Hill =
    let hillTypeOf (hill: Hill) =
        HillType.fromHS hill.HSPoint
        
    let pointsPerMeter (hill: Hill) : HillPointsForMeter =
        HillPointsForMeter.fromK hill.KPoint
       
    let pointsPerKPoint (hill: Hill) : HillPointsForKPoint =
        hill
        |> hillTypeOf
        |> HillPointsForKPoint.fromHillType
