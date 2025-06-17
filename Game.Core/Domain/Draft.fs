namespace Game.Core.Domain

open Ids

type DraftError =
    | AlreadyStarted
    | NotRunning
    | LimitReached
    | JumperTaken

type DraftOrder =
    | Classic
    | Snake
    | RandomSeed of int

type PickTimer =
    | Unlimited
    | Seconds of int

type DraftSettings = {
    Players             : PlayerId list
    Order               : DraftOrder
    MaxJumpersPerPlayer : uint32
    UniqueJumpers       : bool
    PickTimeout         : PickTimer
}

type DraftChoices = Map<PlayerId, JumperId list>

type DraftProgress =
    | NotStarted
    | Running of player: PlayerId * choices: DraftChoices
    | Done    of DraftChoices

type Draft = {
    Id       : DraftId
    Settings : DraftSettings
    Progress : DraftProgress
}

[<RequireQualifiedAccess>]
module private Internal =
    
    let shuffle seed (xs: 'a list) =
        let rng = System.Random(seed)
        xs |> List.sortBy (fun _ -> rng.Next())

    let totalPicks (choices: DraftChoices) =
        choices |> Seq.sumBy (fun kv -> kv.Value.Length)

    let currentRound players choices =
        totalPicks choices / List.length players

    let picksOf player (choices: DraftChoices) =
        choices |> Map.tryFind player |> Option.map List.length |> Option.defaultValue 0

    let nextPlayer order (players: PlayerId list) turn choices =
        let round = currentRound players choices

        let inRoundPlayers =
            match order with
            | RandomSeed seed -> shuffle (seed + round) players 
            | _               -> players

        let idx = inRoundPlayers |> List.findIndex ((=) turn)

        match order with
        | Classic
        | RandomSeed _ ->
            inRoundPlayers.[(idx + 1) % inRoundPlayers.Length]
        // | Snake ->
        //     let dir  = if round % 2 = 0 then 1 else -1
        //     let edge = if dir = 1 then players.Length-1 else 0
        //     if idx = edge then players.[idx]           // podwójny ruch na krańcu
        //     else players.[idx + dir]
        | Snake ->
            let picksInRound = players.Length + 1
            let round        = totalPicks choices / picksInRound

            let dir  = if round % 2 = 0 then 1 else -1
            let edge = if dir = 1 then players.Length - 1 else 0

            if idx = edge then players.[idx]
            else players.[idx + dir]


module Draft =
    let create (settings: DraftSettings) : Draft =
        { Id       = Id.newDraftId()
          Settings = settings
          Progress = NotStarted }

    let start (draft: Draft) : Result<Draft, DraftError> =
        match draft.Progress with
        | NotStarted ->
            let first = List.head draft.Settings.Players
            let empty = draft.Settings.Players |> List.map (fun p -> p, []) |> Map.ofList
            Ok { draft with Progress = Running(first, empty) }
        | _ -> Error AlreadyStarted

    let pick
        (jumperId: JumperId)
        (draft   : Draft)
        : Result<Draft, DraftError> =

        let settings = draft.Settings

        let duplicate jmp choices =
            choices |> Map.exists (fun _ -> List.contains jmp)

        match draft.Progress with
        | Running (turn, choices) ->
            if Internal.picksOf turn choices >= int settings.MaxJumpersPerPlayer then
                Error LimitReached

            elif settings.UniqueJumpers && duplicate jumperId choices then
                Error JumperTaken

            else
                let updated =
                    choices
                    |> Map.change turn (function
                        | Some list -> Some (jumperId :: list)
                        | None      -> Some [jumperId])

                let finished =
                    updated
                    |> Map.forall (fun _ js ->
                        js.Length = int settings.MaxJumpersPerPlayer)

                if finished then
                    Ok { draft with Progress = Done updated }
                else
                    let next =
                        Internal.nextPlayer settings.Order settings.Players turn updated
                    Ok { draft with Progress = Running(next, updated) }

        | NotStarted -> Error NotRunning
        | Done _     -> Error NotRunning
