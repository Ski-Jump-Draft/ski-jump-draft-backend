namespace Game.Core.Domain.Draft

open Game.Core.Domain.Shared
open Game.Core.Domain.Shared.Ids

module Draft =
    type Error =
        | AlreadyStarted
        | NotRunning
        | LimitReached
        | JumperTaken

    type Order =
        | Classic
        | Snake
        | RandomSeed of int

    type PickTimer =
        | Unlimited
        | Seconds of int

    type Settings =
        { Players: PlayerId list
          Order: Order
          MaxJumpersPerPlayer: uint32
          UniqueJumpers: bool
          PickTimeout: PickTimer }

    type Choices = Map<PlayerId, JumperId list>

    type Progress =
        | NotStarted
        | Running of player: PlayerId * choices: Choices
        | Done of Choices

    type Definition =
        { Id: DraftId
          Settings: Settings
          Progress: Progress }

    // ----------  private helpers  ----------

    [<RequireQualifiedAccess>]
    module private Internal =

        let shuffle seed (xs: 'a list) =
            let rng = System.Random seed
            xs |> List.sortBy (fun _ -> rng.Next())

        let totalPicks (choices: Choices) =
            choices |> Seq.sumBy (fun kv -> kv.Value.Length)

        let currentRound players choices =
            totalPicks choices / List.length players

        let picksOf player (choices: Choices) =
            choices |> Map.tryFind player |> Option.map List.length |> Option.defaultValue 0

        let nextPlayer order (players: PlayerId list) turn choices =
            let round = currentRound players choices

            let inRoundPlayers =
                match order with
                | RandomSeed seed -> shuffle (seed + round) players
                | _ -> players

            let idx = inRoundPlayers |> List.findIndex ((=) turn)

            match order with
            | Classic
            | RandomSeed _ -> inRoundPlayers.[(idx + 1) % inRoundPlayers.Length]

            | Snake ->
                let picksInRound = players.Length + 1
                let round = totalPicks choices / picksInRound

                let dir = if round % 2 = 0 then 1 else -1
                let edge = if dir = 1 then players.Length - 1 else 0

                if idx = edge then
                    players.[idx] // podwójny ruch na krańcu
                else
                    players.[idx + dir]

    // ----------  public API  ----------

    let create (settings: Settings) : Definition =
        { Id = Id.newDraftId ()
          Settings = settings
          Progress = Progress.NotStarted }

    let start (draft: Definition) : Result<Definition, Error> =
        match draft.Progress with
        | Progress.NotStarted ->
            let first = List.head draft.Settings.Players
            let empty = draft.Settings.Players |> List.map (fun p -> p, []) |> Map.ofList

            Ok
                { draft with
                    Progress = Progress.Running(first, empty) }

        | _ -> Error AlreadyStarted

    let pick (jumperId: JumperId) (draft: Definition) : Result<Definition, Error> =

        let settings = draft.Settings

        let duplicate jmp choices =
            choices |> Map.exists (fun _ -> List.contains jmp)

        match draft.Progress with
        | Progress.Running(turn, choices) ->

            if Internal.picksOf turn choices >= int settings.MaxJumpersPerPlayer then
                Error LimitReached

            elif settings.UniqueJumpers && duplicate jumperId choices then
                Error JumperTaken

            else
                let updated =
                    choices
                    |> Map.change turn (function
                        | Some list -> Some(jumperId :: list)
                        | None -> Some [ jumperId ])

                let finished =
                    updated |> Map.forall (fun _ js -> js.Length = int settings.MaxJumpersPerPlayer)

                if finished then
                    Ok
                        { draft with
                            Progress = Progress.Done updated }
                else
                    let next = Internal.nextPlayer settings.Order settings.Players turn updated

                    Ok
                        { draft with
                            Progress = Progress.Running(next, updated) }

        | Progress.NotStarted
        | Progress.Done _ -> Error NotRunning
