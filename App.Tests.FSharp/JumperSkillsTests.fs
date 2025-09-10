module JumperSkillsTests

open App.Domain.GameWorld
open Xunit
open FsUnit.Xunit
open App.Domain.GameWorld.JumperSkills
open Microsoft.FSharp.Core

let getOk = function
  | Ok v    -> v
  | Error _ -> failwith "Expected Ok, got Error"

// -- BigSkill --
[<Fact>]
let ``BigSkill accepts 3.0`` () =
    BigSkill.tryCreate 3.0
    |> Result.isOk
    |> should equal true

[<Fact>]
let ``BigSkill rejects 0.0 and -1.0`` () =
    BigSkill.tryCreate 0.0 |> Result.isError |> should equal true
    BigSkill.tryCreate -1.0 |> Result.isError |> should equal true

[<Fact>]
let ``LiveForm accepts 0..10`` () =
    for i in -1..11 do
        let ok = LiveForm.tryCreate i |> Result.isOk
        if i>=0 && i<=10 then ok |> should equal true
        else ok |> should equal false

[<Fact>]
let ``JumperSkills constructs with valid data`` () =
    let takeoff = getOk (BigSkill.tryCreate 4.0)
    let flight  = getOk (BigSkill.tryCreate 5.5)
    let form    = getOk (LiveForm.tryCreate 7)
    let landing = getOk (LandingSkill.tryCreate 1)
    let skills =
      {
        Takeoff = takeoff
        Flight  = flight
        Landing = landing
        LiveForm = form }

    BigSkill.value skills.Flight |> should equal 5.5
    skills.Landing |> should equal landing
