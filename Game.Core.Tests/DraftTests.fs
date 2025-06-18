module DraftTests

open TestHelpers
open Xunit
open FsUnit.Xunit
open Game.Core.Domain
open Game.Core.Domain.Draft
open Game.Core.Domain.Shared.Ids

// --- dane testowe ---
let playerA, playerB, playerC = PlayerId "A", PlayerId "B", PlayerId "C"

let jumperX, jumperY, jumperZ = JumperId "X", JumperId "Y", JumperId "Z"

let baseSettings =
    { Players = [ playerA; playerB; playerC ]
      Order = Classic
      MaxJumpersPerPlayer = 2u
      UniqueJumpers = true
      PickTimeout = PickTimer.Unlimited }

// ---------- Classic ----------
[<Fact>]
let ``Classic rota A→B→C→A`` () =
    let draft = create baseSettings |> start |> getOk
    let after1 = pick jumperX draft |> getOk // A
    let after2 = pick jumperY after1 |> getOk // B
    let after3 = pick jumperZ after2 |> getOk // C

    match after3.Progress with
    | Running(next, _) -> next |> should equal playerA
    | _ -> failwith "nie Running"

// ---------- Snake ----------
[<Fact>]
let ``Snake odwrotnie w rundzie 2`` () =
    let snakeSet = { baseSettings with Order = Snake }
    let d0 = Draft.create snakeSet |> start |> getOk
    let d1 = pick jumperX d0 |> getOk // A
    let d2 = pick jumperY d1 |> getOk // B
    let d3 = pick jumperZ d2 |> getOk // C

    match d3.Progress with
    | Running(next, _) -> next |> should equal playerC
    | _ -> failwith "nie Running"

// ---------- Unikalność ----------
[<Fact>]
let ``Duplikat jumpera blokuje wybór`` () =
    let d0 = Draft.create baseSettings |> start |> getOk
    let d1 = pick jumperX d0 |> getOk
    pick jumperX d1
        |> should equal (Error JumperTaken : Result<Draft, DraftError>)

// ---------- RandomSeed ----------
[<Fact>]
let ``RandomSeed tasuje kolejkę co rundę`` () =
    let rndSet =
        { baseSettings with
            Order = RandomSeed 42 }

    let d0 = create rndSet |> start |> getOk

    let firstStarter = // gracz rundy 1
        match d0.Progress with
        | Running(p, _) -> p
        | _ -> failwith "!"

    // A, B, C wybierają
    d0
    |> pick (JumperId "A")
    |> getOk
    |> pick (JumperId "B")
    |> getOk
    |> pick (JumperId "C")
    |> function
        | Ok d3 ->
            match d3.Progress with
            | Running(starter2, _) -> starter2 |> should not' (equal firstStarter)
            | _ -> failwith "nie Running"
        | Error e -> failwithf $"%A{e}"
