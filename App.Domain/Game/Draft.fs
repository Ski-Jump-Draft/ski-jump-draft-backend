namespace App.Domain.Game

open System
open System.Collections
open System.Collections.Generic

type DraftError =
    | DraftAlreadyEnded
    | InvalidPlayer
    | JumperNotAllowed
    | MaxPicksReached
    | NotEnoughJumpers
    | JumperNotFound
    | Other of string

module Draft =
    type Picks = Map<PlayerId, JumperId list>

    module SettingsModule =
        type TargetPicks = private TargetPicks of int

        module TargetPicks =
            let create (v: int) =
                if v > 0 && v < 100 then Some(TargetPicks v) else None

            let value (TargetPicks v) = v

        type MaxPicks = private MaxPicks of int

        module MaxPicks =
            let create (v: int) =
                if v > 0 && v < 100 then Some(MaxPicks v) else None

            let value (MaxPicks v) = v

        type UniqueJumpersPolicy =
            | Unique
            | NotUnique

        type Order =
            | Classic
            | Snake
            | Random

        type TimeoutTime = private TimeoutTime of TimeSpan

        module TimeoutTime =
            let tryCreate (v: TimeSpan) =
                if v >= TimeSpan.FromSeconds(10L) && v <= TimeSpan.FromSeconds(60L) then
                    Some(TimeoutTime v)
                else
                    None

        type TimeoutPolicy =
            | NoTimeout
            | TimeoutAfter of Time: TimeSpan

    type Settings =
        { TargetPicks: SettingsModule.TargetPicks
          MaxPicks: SettingsModule.MaxPicks
          UniqueJumpersPolicy: SettingsModule.UniqueJumpersPolicy
          Order: SettingsModule.Order
          TimeoutPolicy: SettingsModule.TimeoutPolicy }

    type TurnIndex = private TurnIndex of int

    module TurnIndex =
        let create v =
            if v >= 0 then Some(TurnIndex v) else None

        let value (TurnIndex v) = v
        let zero = TurnIndex 0
        let next (TurnIndex v) = TurnIndex(v + 1)

    type Turn =
        { Index: TurnIndex; PlayerId: PlayerId }


open Draft
open Draft.SettingsModule

type Draft =
    private
        { Settings: Draft.Settings
          Picks: Picks
          AllJumpers: Set<JumperId>
          TurnOrder: PlayerId list
          CurrentTurnIndex: TurnIndex
          PicksPerPlayer: int
          TotalPicks: int }

    /// Fisher–Yates on array
    static member private shuffle (rng: Random) (items: 'a list) =
        let arr = items |> List.toArray
        let n = arr.Length

        for i in n - 1 .. -1 .. 1 do
            let j = rng.Next(i + 1)
            let tmp = arr.[i]
            arr.[i] <- arr.[j]
            arr.[j] <- tmp

        arr |> Array.toList

    static member Create
        (settings: Draft.Settings)
        (players: PlayerId list)
        (jumpers: JumperId list)
        (shuffleFn: (PlayerId list * int -> PlayerId list) option)
        : Result<Draft, DraftError> =

        let playerCount = players.Length
        let jumperCount = jumpers.Length

        if playerCount = 0 then
            Error(Other "No players provided")
        else
            let target = SettingsModule.TargetPicks.value settings.TargetPicks

            let picksPerPlayer =
                let byDivision = jumperCount / playerCount
                min target byDivision

            if picksPerPlayer < 1 then
                Error NotEnoughJumpers
            else
                // build baseOrder / per-round orders depending on Order
                let buildTurnOrder () : Result<PlayerId list, DraftError> =
                    match settings.Order with
                    | Classic -> Ok(List.init picksPerPlayer (fun _ -> players) |> List.concat)
                    | Snake ->
                        let rounds = [ 0 .. picksPerPlayer - 1 ]

                        let turnOrder =
                            rounds
                            |> List.collect (fun r -> if r % 2 = 0 then players else List.rev players)

                        Ok turnOrder
                    | Random ->
                        match shuffleFn with
                        | None -> Error(Other "Random order requires shuffleFn (inject via application layer)")
                        | Some shuffle ->
                            // shuffle each round separately
                            let rounds = [ 0 .. picksPerPlayer - 1 ]
                            let perRound = rounds |> List.map (fun _ -> shuffle (players, 0))
                            Ok(perRound |> List.concat)

                match buildTurnOrder () with
                | Error e -> Error e
                | Ok turnOrder ->
                    let allJumpersSet = jumpers |> Set.ofList
                    let totalPicks = picksPerPlayer * playerCount

                    let draft =
                        { Settings = settings
                          Picks = Map.empty
                          AllJumpers = allJumpersSet
                          TurnOrder = turnOrder
                          CurrentTurnIndex = TurnIndex.zero
                          PicksPerPlayer = picksPerPlayer
                          TotalPicks = totalPicks }

                    Ok draft

    member this.Ended: bool =
        let i = TurnIndex.value this.CurrentTurnIndex
        let finishedByTurnOrder = i >= this.TurnOrder.Length
        let picksMade = this.Picks |> Seq.sumBy (fun kv -> kv.Value.Length)
        let everyoneDone = picksMade >= this.TotalPicks
        finishedByTurnOrder || everyoneDone


    member this.CurrentTurn: Turn option =
        if this.Ended then
            None
        else
            this.TurnOrder
            |> List.tryItem (TurnIndex.value this.CurrentTurnIndex)
            |> Option.map (fun pid ->
                { Index = this.CurrentTurnIndex
                  PlayerId = pid })

    member this.PicksOf(playerId: PlayerId) : JumperId list option = this.Picks |> Map.tryFind playerId

    member this.AllPicks: Picks = this.Picks

    member this.CanBePicked(jumperId: JumperId) : bool =
        if not (this.AllJumpers.Contains jumperId) then
            false
        else
            match this.Settings.UniqueJumpersPolicy with
            | Unique -> not (this.Picks |> Map.exists (fun _ picks -> picks |> List.contains jumperId))
            | NotUnique -> true

    member this.Pick (playerId: PlayerId) (jumperId: JumperId) : Result<Draft, DraftError> =
        if this.Ended then
            Error DraftAlreadyEnded
        else
            match this.CurrentTurn with
            | None -> Error DraftAlreadyEnded
            | Some t when t.PlayerId <> playerId -> Error InvalidPlayer
            | Some _ when not (this.AllJumpers.Contains jumperId) -> Error JumperNotFound
            | Some _ when not (this.CanBePicked jumperId) -> Error JumperNotAllowed
            | Some _ ->
                let playerPicks = this.Picks |> Map.tryFind playerId |> Option.defaultValue []

                if playerPicks |> List.contains jumperId then
                    Error(JumperNotAllowed)
                else
                    // POPRAWKA: Sprawdź zarówno PicksPerPlayer jak i MaxPicks
                    let maxAllowed =
                        min this.PicksPerPlayer (SettingsModule.MaxPicks.value this.Settings.MaxPicks)

                    if playerPicks.Length >= maxAllowed then
                        Error MaxPicksReached
                    else
                        let updated = this.Picks |> Map.add playerId (playerPicks @ [ jumperId ])

                        Ok
                            { this with
                                Picks = updated
                                CurrentTurnIndex = TurnIndex.next this.CurrentTurnIndex }

    // member this.Pick (playerId: PlayerId) (jumperId: JumperId) : Result<Draft, DraftError> =
    //     if this.Ended then
    //         Error DraftAlreadyEnded
    //     else
    //         match this.CurrentTurn with
    //         | None -> Error DraftAlreadyEnded
    //         | Some t when t.PlayerId <> playerId -> Error InvalidPlayer
    //         | Some _ when not (this.AllJumpers.Contains jumperId) -> Error JumperNotFound
    //         | Some _ when not (this.CanBePicked jumperId) -> Error JumperNotAllowed
    //         | Some _ ->
    //             let playerPicks =
    //                 this.Picks |> Map.tryFind playerId |> Option.defaultValue Set.empty
    //
    //             if playerPicks.Count >= this.PicksPerPlayer then
    //                 Error MaxPicksReached
    //             else
    //                 let updated = this.Picks |> Map.add playerId (playerPicks.Add jumperId)
    //
    //                 Ok
    //                     { this with
    //                         Picks = updated
    //                         CurrentTurnIndex = TurnIndex.next this.CurrentTurnIndex }

    member this.AvailablePicks: Set<JumperId> =
        match this.Settings.UniqueJumpersPolicy with
        | Unique ->
            let taken = this.Picks |> Seq.collect (fun kv -> kv.Value) |> Set.ofSeq
            this.AllJumpers - taken
        | NotUnique -> this.AllJumpers

    member this.TurnQueueRemaining: PlayerId list =
        if this.Ended then
            []
        else
            let i = TurnIndex.value this.CurrentTurnIndex
            this.TurnOrder |> List.skip i

    // member this.PicksUntil(pid: PlayerId) : int option =
    //     if this.Ended then
    //         None
    //     else
    //         let i = TurnIndex.value this.CurrentTurnIndex
    //
    //         match
    //             this.TurnOrder
    //             |> List.tryFindIndex (fun p -> p = pid)
    //             |> Option.map (fun j -> j - i)
    //         with
    //         | Some d when d >= 0 -> Some d
    //         | _ -> None

    member this.PicksUntil(pid: PlayerId) : int option =
        if this.Ended then
            None
        else
            let i = TurnIndex.value this.CurrentTurnIndex
            let remainingOrder = this.TurnOrder |> List.skip i
            remainingOrder |> List.tryFindIndex (fun p -> p = pid)
