module HillModuleTests

open System
open App.Domain.Shared
open Xunit
open FsUnit.Xunit
open App.Domain
open App.Domain.GameWorld
open App.Domain.GameWorld.Hill

// ───────────────────────────────────────────────
//  Dane pomocnicze
// ───────────────────────────────────────────────
let k x  = KPoint.tryCreate  x |> Option.get   // ufamy, że dodatnie
let hs x = HSPoint.tryCreate x |> Option.get

let fixedGuid = Guid.Parse "00000000-0000-0000-0000-00000000000F"
let idGen =
  { new IGuid with
      member _.NewGuid() = fixedGuid }

let dummyCountry = Country.Id(idGen.NewGuid())

let dummyHill (kVal:double) (hsVal:double) : Hill =
    { Id        = Hill.Id(idGen.NewGuid())
      Name      = Name "Test"
      Location  = Location "Nowhere"
      CountryId = dummyCountry
      KPoint    = k kVal
      HSPoint   = hs hsVal
}

// ───────────────────────────────────────────────
//  1. HillType.fromHS
// ───────────────────────────────────────────────
[<Fact>]
let ``HS -> HillType klasyfikacja`` () =
    let cases : (double * Type) list = [
        30.0 , Small
        70.0 , Medium
        100.0, Normal
        140.0, Large
        180.0, Big
        225.0, SkiFlying
    ]

    for (hs, expected) in cases do
        let result = Type.fromHS (HSPoint.tryCreate hs |> Option.get)
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
    let (PointsForMeter pts) = PointsForMeter.fromK (k kVal)
    pts |> should equal expected

// ───────────────────────────────────────────────
//  3. Hill.pointsPerMeter  (agregatowa)
// ───────────────────────────────────────────────
[<Fact>]
let ``pointsPerMeter zwraca to samo co HillPointsForMeter.fromK`` () =
    let hill = dummyHill 95.0 110.0
    let (PointsForMeter direct)  = PointsForMeter.fromK hill.KPoint
    let (PointsForMeter viaHill) = hill.PointsPerMeter
    viaHill |> should equal direct

// ───────────────────────────────────────────────
//  4. Hill.pointsPerKPoint  (60 lub 120)
// ───────────────────────────────────────────────
[<Theory>]
[<InlineData (95.0 ,110.0, 60.0 )>]   // Big = 60
[<InlineData (185.0,220.0,120.0)>]   // SkiFlying = 120
let ``pointsPerKPoint zależne od HillType`` (kVal, hsVal, expected) =
    let hill = dummyHill kVal hsVal
    let (PointsForKPoint pts) = hill.PointsPerKPoint
    pts |> should equal expected
