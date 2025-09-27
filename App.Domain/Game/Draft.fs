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
        
    member this.FullTurnOrder : PlayerId list = this.TurnOrder

    static member private buildTurnOrder
        (order: SettingsModule.Order)
        (players: PlayerId list)
        (rounds: int)
        (shuffleFn: (PlayerId list * int -> PlayerId list) option)
        (turnOrderOverride: PlayerId list option)
        : Result<PlayerId list, DraftError> =
        match order with
        | Classic -> Ok(List.replicate rounds players |> List.concat)
        | Snake ->
            let rs = [ 0 .. rounds - 1 ]
            Ok(rs |> List.collect (fun r -> if r % 2 = 0 then players else List.rev players))
        | Random ->
            match turnOrderOverride with
            | Some o ->
                if o.Length = rounds * players.Length then
                    Ok o
                else
                    Error(Other "TurnOrder length mismatch for Random")
            | None ->
                match shuffleFn with
                | None -> Error(Other "Random requires persisted turnOrder (or deterministic shuffle) to restore")
                | Some shuffle ->
                    let rs = [ 0 .. rounds - 1 ]
                    let perRound = rs |> List.map (fun r -> shuffle (players, r))
                    Ok(perRound |> List.concat)

    static member private make
        (settings: Draft.Settings)
        (players: PlayerId list)
        (jumpers: JumperId list)
        (picks: Picks)
        (turnOrder: PlayerId list)
        : Result<Draft, DraftError> =

        let allJumpersSet = jumpers |> Set.ofList

        // weryfikacje proste
        let allPicked = picks |> Seq.collect (fun kv -> kv.Value) |> List.ofSeq

        if allPicked |> List.exists (fun j -> not (allJumpersSet.Contains j)) then
            Error JumperNotFound
        else
            match settings.UniqueJumpersPolicy with
            | Unique when (allPicked.Length <> (allPicked |> Set.ofList |> Set.count)) -> Error JumperNotAllowed
            | _ ->
                let maxPer = turnOrder.Length / players.Length

                if picks |> Seq.exists (fun kv -> kv.Value.Length > maxPer) then
                    Error MaxPicksReached
                else
                    let curIdx = min allPicked.Length turnOrder.Length |> TurnIndex.create |> Option.get

                    Ok
                        { Settings = settings
                          Picks = picks
                          AllJumpers = allJumpersSet
                          TurnOrder = turnOrder
                          CurrentTurnIndex = curIdx
                          PicksPerPlayer = maxPer
                          TotalPicks = turnOrder.Length }

    static member Create
        (settings: Draft.Settings)
        (players: PlayerId list)
        (jumpers: JumperId list)
        (shuffleFn: (PlayerId list * int -> PlayerId list) option)
        : Result<Draft, DraftError> =

        let pc = players.Length
        let jc = jumpers.Length
        if pc = 0 then Error(Other "No players provided") else
        let target = SettingsModule.TargetPicks.value settings.TargetPicks
        let byDivision = jc / pc
        let picksPerPlayerBase = min target byDivision
        if picksPerPlayerBase < 1 then Error NotEnoughJumpers else

        let effectiveRounds = min picksPerPlayerBase (SettingsModule.MaxPicks.value settings.MaxPicks)

        Draft.buildTurnOrder settings.Order players effectiveRounds shuffleFn None
        |> Result.bind (fun order ->
            Draft.make settings players jumpers Map.empty order)

    static member Restore
        (settings: Draft.Settings)
        (players: PlayerId list)
        (jumpers: JumperId list)
        (picks: Picks)
        (turnOrderOpt: PlayerId list option)
        : Result<Draft, DraftError> =

        let pc = players.Length
        let jc = jumpers.Length

        if pc = 0 then
            Error(Other "No players")
        else

            let target = SettingsModule.TargetPicks.value settings.TargetPicks
            let byDiv = jc / pc
            let picksPerPlayerBase = min target byDiv

            if picksPerPlayerBase < 1 then
                Error NotEnoughJumpers
            else

                let effectiveRounds =
                    min picksPerPlayerBase (SettingsModule.MaxPicks.value settings.MaxPicks)

                let orderRes =
                    match settings.Order with
                    | Classic -> Draft.buildTurnOrder Classic players effectiveRounds None None
                    | Snake -> Draft.buildTurnOrder Snake players effectiveRounds None None
                    | Random ->
                        match turnOrderOpt with
                        | Some o -> Draft.buildTurnOrder Random players effectiveRounds None (Some o)
                        | None -> Error(Other "Random order requires persisted turnOrder")

                orderRes
                |> Result.bind (fun order -> Draft.make settings players jumpers picks order)


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
