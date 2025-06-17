module HillTests

open Xunit
open FsUnit.Xunit
open Game.Core.Domain
open Game.Core.Domain.HillType
open Game.Core.Domain.HillPointsForMeter
open Game.Core.Domain.HillPointsForKPoint

// ───────────────────────────────────────────────
//  Dane pomocnicze
// ───────────────────────────────────────────────
let k x  = KPoint.tryCreate  x |> Option.get   // ufamy, że dodatnie
let hs x = HSPoint.tryCreate x |> Option.get

let dummyHill (kVal:float) (hsVal:float) : Hill =
    { Id        = Id.newHillId()
      Name      = HillName "Test"
      Location  = HillLocation "Nowhere"
      CountryCode = "PL" |> CountryCode.tryCreate |> Option.get
      KPoint    = k kVal
      HSPoint   = hs hsVal
      SimulationSettings = Unchecked.defaultof<HillSimulationSettings> }

// ───────────────────────────────────────────────
//  1. HillType.fromHS
// ───────────────────────────────────────────────
[<Fact>]
let ``HS -> HillType klasyfikacja`` () =
    let cases : (float * HillType) list = [
        30.0 , Small
        70.0 , Medium
        100.0, Normal
        140.0, Large
        180.0, Big
        225.0, SkiFlying
    ]

    for (hs, expected) in cases do
        let result = HillType.fromHS (HSPoint.tryCreate hs |> Option.get)
        result |> should equal expected

// ───────────────────────────────────────────────
//  2. HillPointsForMeter.fromK
// ───────────────────────────────────────────────
[<Theory>]
[<InlineData (20.0, 4.8)>]
[<InlineData (45.0, 3.2)>]
[<InlineData (65.0, 2.4)>]
[<InlineData (120.0,1.8)>]
[<InlineData (170.0,1.6)>]
[<InlineData (200.0,1.2)>]
let ``Punkty za metr zależą od K-point`` (kVal, expected) =
    let (HillPointsForMeter pts) = fromK (k kVal)
    pts |> should equal expected

// ───────────────────────────────────────────────
//  3. Hill.pointsPerMeter  (agregatowa)
// ───────────────────────────────────────────────
[<Fact>]
let ``pointsPerMeter zwraca to samo co HillPointsForMeter.fromK`` () =
    let hill = dummyHill 95.0 110.0
    let (HillPointsForMeter direct)  = fromK hill.KPoint
    let (HillPointsForMeter viaHill) = Hill.pointsPerMeter hill
    viaHill |> should equal direct

// ───────────────────────────────────────────────
//  4. Hill.pointsPerKPoint  (60 lub 120)
// ───────────────────────────────────────────────
[<Theory>]
[<InlineData (95.0 ,110.0, 60.0 )>]   // Big = 60
[<InlineData (185.0,220.0,120.0)>]   // SkiFlying = 120
let ``pointsPerKPoint zależne od HillType`` (kVal, hsVal, expected) =
    let hill = dummyHill kVal hsVal
    let (HillPointsForKPoint pts) = Hill.pointsPerKPoint hill
    pts |> should equal expected
