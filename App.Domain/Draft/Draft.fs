namespace App.Domain.Draft

open System
open App.Domain
open App.Domain.Shared
open App.Domain.Shared.Random
open App.Domain.Shared.Utils.Range

// TODO: Osobny player dla draft/competition

module Draft =
    [<Struct>]
    type Id = Id of System.Guid
    
    type Order =
        | Classic
        | Snake
        | RandomSeed of int

    module PickTimeout =
        type FixedTime = private FixedTime of TimeSpan

        module FixedTime =
            type Error = OutsideRange of OutsideRangeError<int>

            let tryCreate (v: TimeSpan) =
                if v >= TimeSpan.FromSeconds(10L) && v <= TimeSpan.FromSeconds(60L) then
                    Ok(FixedTime v)
                else
                    Error(
                        OutsideRange
                            { Min = Some 10
                              Max = Some 60
                              Current = v.Seconds }
                    )

    type PickTimeout =
        | Unlimited
        | Fixed of PickTimeout.FixedTime
        
    module Player =
        type Id = private Id of Guid
        module Id =
            let tryCreate (v: Guid) = Id v
            let value (Id v) = v
            
        type Name = private Name of string
        module Name =
            let tryCreate (v: string) =
                if v.Length >= 3 && v.Length < 25 then
                    Ok(Name v)
                else
                    Error(invalidOp "Name must be 3-24 characters")
            let value (Name v) = v
    
    type Player = {
        Id: Player.Id
        Name: Player.Name
    }

    type Settings =
        { Players: Player.Id list
          Order: Order
          MaxJumpersPerPlayer: uint
          UniqueJumpers: bool
          PickTimeout: PickTimeout }

    type Choices = Map<Player.Id, GameWorld.Jumper.Id list>

    type Progress =
        | NotStarted
        | Running of Turn: Player.Id * Choices
        | Done of Choices

    type Error =
        | AlreadyStarted
        | NotRunning
        | LimitReached
        | JumperTaken

open Draft

[<RequireQualifiedAccess>]
module private Internal =
    let totalPicks (choices: Choices) =
        choices |> Seq.sumBy (fun kv -> kv.Value.Length)

    let currentRound players choices =
        totalPicks choices / List.length players

    let picksOf player (choices: Choices) =
        choices |> Map.tryFind player |> Option.map List.length |> Option.defaultValue 0

    let nextPlayer order players turn choices (random: IRandom )=
        let round = currentRound players choices

        let inRound =
            match order with
            | RandomSeed seed -> random.ShuffleList (seed + round) players
            | _ -> players

        let idx = inRound |> List.findIndex ((=) turn)

        match order with
        | Classic
        | RandomSeed _ -> inRound.[(idx + 1) % inRound.Length]
        | Snake ->
            let picksInRound = players.Length + 1
            let round' = totalPicks choices / picksInRound
            let dir = if round' % 2 = 0 then 1 else -1
            let edge = if dir = 1 then players.Length - 1 else 0
            if idx = edge then players.[idx] else players.[idx + dir]

type Draft =
    { Id: Draft.Id
      Settings: Settings
      Progress: Progress
      Random: IRandom }

    static member Create (idGen: IGuid) (settings: Settings) (random: IRandom) : Draft =
        { Id = Id(idGen.NewGuid())
          Settings = settings
          Progress = Progress.NotStarted
          Random = random }

    member this.Start: Result<Draft, Error> =
        match this.Progress with
        | Progress.NotStarted ->
            let first = List.head this.Settings.Players
            let empty = this.Settings.Players |> List.map (fun p -> p, []) |> Map.ofList

            { this with
                Progress = Progress.Running(first, empty) }
            |> Ok
        | _ -> Error AlreadyStarted

    member this.Pick(jumperId: GameWorld.Jumper.Id) : Result<Draft, Error> =
        let s = this.Settings

        let duplicate jmp choices =
            choices |> Map.exists (fun _ -> List.contains jmp)

        match this.Progress with
        | Progress.Running(turn, choices) ->
            if Internal.picksOf turn choices >= int s.MaxJumpersPerPlayer then
                Error LimitReached
            elif s.UniqueJumpers && duplicate jumperId choices then
                Error JumperTaken
            else
                let choices =
                    choices
                    |> Map.change turn (function
                        | Some xs -> Some(jumperId :: xs)
                        | None -> Some [ jumperId ])

                let finished =
                    choices |> Map.forall (fun _ xs -> xs.Length = int s.MaxJumpersPerPlayer)

                if finished then
                    { this with
                        Progress = Progress.Done choices }
                    |> Ok
                else
                    let currentTurn = Internal.nextPlayer s.Order s.Players turn choices this.Random

                    { this with
                        Progress = Progress.Running(currentTurn, choices) }
                    |> Ok
        | Progress.NotStarted
        | Progress.Done _ -> Error NotRunning
