module DraftTests

open System
open App.Domain.GameWorld
open App.Domain.Shared.Random
open TestHelpers
open Xunit
open FsUnit.Xunit

open App.Domain
open App.Domain.Draft
open App.Domain.Shared
open App.Domain.Shared.Ids

let fixedGuid = Guid.Parse "00000000-0000-0000-0000-00000000000F"

let idGen =
    { new IGuid with
        member _.NewGuid() = fixedGuid }

let guidA = Guid.Parse "00000000-0000-0000-0000-000000000001"
let guidB = Guid.Parse "00000000-0000-0000-0000-000000000002"
let guidC = Guid.Parse "00000000-0000-0000-0000-000000000003"

let jumpX = Guid.Parse "00000000-0000-0000-0000-000000000010"
let jumpY = Guid.Parse "00000000-0000-0000-0000-000000000011"
let jumpZ = Guid.Parse "00000000-0000-0000-0000-000000000012"

let playerA, playerB, playerC = Draft.Participant.Id(guidA), Draft.Participant.Id(guidB), Draft.Participant.Id(guidC)

let jumperX, jumperY, jumperZ = JumperId jumpX, JumperId jumpY, JumperId jumpZ

let identityRandom =
    { new IRandom with
        member _.ShuffleList _ xs = xs }

let stubRandom =
    { new IRandom with
        member _.ShuffleList seed xs =
            match seed with
            | 2137 -> [ xs.[1]; xs.[2]; xs.[0] ] // [B;C;A] — runda 1: A→B→C
            | 2138 -> [ xs.[2]; xs.[1]; xs.[0] ] // [C;B;A] — runda 2: C→B→A
            | _ -> xs }

let baseSettings: Draft.Settings =
    { Players = [ playerA; playerB; playerC ]
      Order = Draft.Order.Classic
      MaxJumpersPerPlayer = 2u
      UniqueJumpers = true
      PickTimeout = Draft.PickTimeout.Unlimited }

// ---------- Classic ----------
[<Fact>]
let ``Classic rota A→B→C→A`` () =
    let draft = (Draft.Draft.Create idGen baseSettings identityRandom).Start |> getOk
    let after1 = draft.Pick jumperX |> getOk // A
    let after2 = draft.Pick jumperY |> getOk // B
    let after3 = draft.Pick jumperZ |> getOk // C

    match after3.Phase with
    | Draft.Running(currentTurn, _) -> currentTurn |> should equal playerA
    | _ -> failwith "nie Running"

// ---------- Snake ----------
[<Fact>]
let ``Snake odwrotnie w rundzie 2`` () =
    let snakeSet =
        { baseSettings with
            Order = Draft.Order.Snake }

    let d0 = (Draft.Draft.Create idGen baseSettings identityRandom).Start |> getOk
    let d1 = d0.Pick jumperX |> getOk // A
    let d2 = d1.Pick jumperY |> getOk // B
    let d3 = d2.Pick jumperZ |> getOk // C

    match d3.Phase with
    | Draft.Running(currentTurn, _) -> currentTurn |> should equal playerC
    | _ -> failwith "nie Running"

// ---------- Unikalność ----------
[<Fact>]
let ``Duplikat jumpera blokuje wybór`` () =
    let d0 = (Draft.Draft.Create idGen baseSettings identityRandom).Start |> getOk
    let d1 = d0.Pick jumperX |> getOk

    d1.Pick jumperX
    |> should equal (Error Draft.Error.JumperTaken: Result<Draft, Draft.Error>)

// ---------- RandomSeed ----------
[<Fact>]
let ``RandomSeed tasuje kolejkę co rundę`` () =
    let rndSet =
        { baseSettings with
            Order = Draft.Order.RandomSeed 2137 }

    let d0 = (Draft.Draft.Create idGen rndSet stubRandom).Start |> getOk

    let firstStarter = // gracz rundy 1
        match d0.Phase with
        | Draft.Running(currentTurn, _) -> currentTurn
        | _ -> failwith "!"

    let pick (jmp: JumperId) (d: Draft) = d.Pick jmp

    d0
    |> pick jumperX
    |> getOk
    |> pick jumperY
    |> getOk
    |> pick jumperZ
    |> function
        | Ok d3 ->
            match d3.Phase with
            | Draft.Running(currentTurn, _) ->
                currentTurn |> should not' (equal firstStarter)
                currentTurn |> should equal playerB
            | _ -> failwith "nie Running"
        | Error e -> failwithf $"%A{e}"
