module JumperTests

open Xunit
open FsUnit.Xunit
open Game.Core.Domain

let get (opt: Option<'a>) =
    match opt with
    | Some v -> v
    | None -> failwith "Expected Some, got None"

// --- JumperBigSkill ---
[<Fact>]
let ``JumperBigSkill accepts positive value`` () =
    JumperBigSkill.tryCreate 3.0 |> should not' (equal None)

[<Fact>]
let ``JumperBigSkill rejects zero or negative`` () =
    JumperBigSkill.tryCreate 0.0 |> should equal None
    JumperBigSkill.tryCreate -1.0 |> should equal None

// --- JumperLiveForm ---
[<Fact>]
let ``JumperLiveForm accepts values from 0 to 10`` () =
    for i in 0..10 do
        JumperLiveForm.tryCreate i |> should not' (equal None)

[<Fact>]
let ``JumperLiveForm rejects values outside 0 to 10`` () =
    JumperLiveForm.tryCreate -1 |> should equal None
    JumperLiveForm.tryCreate 11 |> should equal None

// --- JumperSkills ---
[<Fact>]
let ``JumperSkills constructs with valid data`` () =
    let takeoff = get (JumperBigSkill.tryCreate 4.0)
    let flight  = get (JumperBigSkill.tryCreate 5.5)
    let form    = get (JumperLiveForm.tryCreate 7)
    let skills =
        { Takeoff = takeoff
          Flight  = flight
          Landing = JumperLandingSkill.Good
          LiveForm = form }

    JumperBigSkill.value skills.Flight |> should equal 5.5
    skills.Landing |> should equal JumperLandingSkill.Good
