namespace App.Domain._2.Game

open System

type DraftError =
    | DraftAlreadyEnded
    | InvalidPlayer
    | JumperNotAllowed
    | MaxPicksReached
    | NotEnoughJumpers
    | JumperNotFound
    | Other of string

    
module Draft =
    module Settings =
        type TargetPicks = private TargetPicks of int
        module TargetPicks =
            let create (v: int) =
                if v > 0 && v < 100 then Some (TargetPicks v) else None
            let value (TargetPicks v) = v

        type MaxPicks = private MaxPicks of int
        module MaxPicks =
            let create (v: int) =
                if v > 0 && v < 100 then Some (MaxPicks v) else None
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
        {
            TargetPicks: Settings.TargetPicks
            MaxPicks: Settings.MaxPicks
            UniqueJumpersPolicy: Settings.UniqueJumpersPolicy
            Order: Settings.Order
            TimeoutPolicy: Settings.TimeoutPolicy
        }

open Draft
open Draft.Settings

type Draft = private {
    Settings: Draft.Settings
    Picks: Map<PlayerId, Set<JumperId>>
    AllJumpers: Set<JumperId>
    TurnOrder: PlayerId list
    CurrentTurnIndex: int
    PicksPerPlayer: int
    TotalPicks: int
} with
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
        (shuffleFn: (PlayerId list * int-> PlayerId list) option)
        : Result<Draft, DraftError> =

        let playerCount = players.Length
        let jumperCount = jumpers.Length

        if playerCount = 0 then Error (Other "No players provided")
        else
            let target = Settings.TargetPicks.value settings.TargetPicks
            let picksPerPlayer =
                let byDivision = jumperCount / playerCount
                min target byDivision

            if picksPerPlayer < 1 then Error NotEnoughJumpers
            else
                // build baseOrder / per-round orders depending on Order
                let buildTurnOrder () : Result<PlayerId list, DraftError> =
                    match settings.Order with
                    | Classic ->
                        Ok (List.init picksPerPlayer (fun _ -> players) |> List.concat)
                    | Snake ->
                        let rounds = [0 .. picksPerPlayer - 1]
                        let turnOrder = rounds |> List.collect (fun r -> if r % 2 = 0 then players else List.rev players)
                        Ok turnOrder
                    | Random ->
                        match shuffleFn with
                        | None -> Error (Other "Random order requires shuffleFn (inject via application layer)")
                        | Some shuffle ->
                            // shuffle each round separately
                            let rounds = [0 .. picksPerPlayer - 1]
                            let perRound = rounds |> List.map (fun _ -> shuffle (players, 0))
                            Ok (perRound |> List.concat)

                match buildTurnOrder() with
                | Error e -> Error e
                | Ok turnOrder ->
                    let allJumpersSet = jumpers |> Set.ofList
                    let totalPicks = picksPerPlayer * playerCount
                    let draft =
                        {
                            Settings = settings
                            Picks = Map.empty
                            AllJumpers = allJumpersSet
                            TurnOrder = turnOrder
                            CurrentTurnIndex = 0
                            PicksPerPlayer = picksPerPlayer
                            TotalPicks = totalPicks
                        }
                    Ok draft

    member this.Ended : bool =
        let finishedByTurnOrder = this.CurrentTurnIndex >= this.TurnOrder.Length
        let everyoneDone =
            this.Picks
            |> Map.forall (fun _ picks -> picks.Count >= this.PicksPerPlayer)
        finishedByTurnOrder || everyoneDone

    member this.CurrentPlayer : PlayerId option =
        if this.Ended then None else this.TurnOrder |> List.tryItem this.CurrentTurnIndex

    member this.PicksOf (playerId: PlayerId) : JumperId list option =
        this.Picks |> Map.tryFind playerId |> Option.map Set.toList

    member this.CanBePicked (jumperId: JumperId) : bool =
        if not (this.AllJumpers.Contains jumperId) then false
        else
            match this.Settings.UniqueJumpersPolicy with
            | Unique -> not (this.Picks |> Map.exists (fun _ picks -> picks.Contains jumperId))
            | NotUnique -> true

    member this.Pick (playerId: PlayerId) (jumperId: JumperId) : Result<Draft, DraftError> =
        if this.Ended then Error DraftAlreadyEnded
        else
            match this.CurrentPlayer with
            | None -> Error DraftAlreadyEnded
            | Some expectedPlayer ->
                if expectedPlayer <> playerId then Error InvalidPlayer
                elif not (this.AllJumpers.Contains jumperId) then Error JumperNotFound
                elif not (this.CanBePicked jumperId) then Error JumperNotAllowed
                else
                    let playerPicks = this.Picks |> Map.tryFind playerId |> Option.defaultValue Set.empty
                    if playerPicks.Count >= this.PicksPerPlayer then Error MaxPicksReached
                    else
                        let updatedPicks = playerPicks |> Set.add jumperId
                        let updatedMap = this.Picks |> Map.add playerId updatedPicks
                        let nextIndex = this.CurrentTurnIndex + 1
                        Ok { this with Picks = updatedMap; CurrentTurnIndex = nextIndex }