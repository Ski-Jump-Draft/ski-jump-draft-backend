namespace App.Domain.Matchmaking

open System.Collections.Generic

type MatchmakingId = MatchmakingId of System.Guid

type MatchmakingResult =
    | Succeeded
    | NotEnoughPlayers

type Status =
    | Running
    | Ended of Result: MatchmakingResult
    | Failed of Reason: string

type MatchmakingError =
    | AlreadyJoined
    | NotInMatchmaking
    | InvalidStatus of Status: Status
    | TooManyPlayers

type Matchmaking =
    private
        { Id: MatchmakingId
          Settings: Settings
          Status: Status
          Players: Set<Player> }

    static member Create id settings =
        { Id = id
          Settings = settings
          Status = Running
          Players = Set.empty }


    member this.Id_: MatchmakingId = this.Id
    member this.Status_: Status = this.Status
    member this.Players_: IReadOnlyCollection<Player> = this.Players
    member this.PlayersCount = this.Players.Count
    member this.MinPlayersCount = this.Settings.MinPlayers
    member this.MaxPlayersCount = this.Settings.MaxPlayers

    member this.MinRequiredPlayers =
        let currentPlayers = this.PlayersCount
        let minPlayers = Settings.MinPlayers.value this.Settings.MinPlayers

        if currentPlayers < minPlayers then
            Some(minPlayers - currentPlayers)
        else
            None

    member this.Join(player: Player) : Result<Matchmaking * Player.Nick, MatchmakingError> =
        match this.Status with
        | Running ->
            if this.Players |> Set.exists (fun p -> p.Id = player.Id) then
                Error AlreadyJoined
            elif this.Players.Count >= Settings.MaxPlayers.value this.Settings.MaxPlayers then
                Error TooManyPlayers
            else
                let existingNicks =
                    this.Players |> Seq.map (fun p -> Player.Nick.value p.Nick) |> Set.ofSeq

                let baseNick = Player.Nick.value player.Nick

                let finalNick =
                    if existingNicks.Contains(baseNick) then
                        let rec find i =
                            let candidate = $"{baseNick} ({i})"

                            if existingNicks.Contains(candidate) then
                                find (i + 1)
                            else
                                candidate

                        find 2
                    else
                        baseNick

                let nick' =
                    Player.Nick.createWithSuffix finalNick
                    |> Option.defaultWith (fun () -> failwith "Nick validation error")

                Ok(
                    { this with
                        Players = this.Players.Add { player with Nick = nick' } },
                    nick'
                )
        | _ -> Error(InvalidStatus this.Status)


    member this.Leave playerId =
        match this.Status with
        | Running ->
            if this.Players |> Set.exists (fun p -> p.Id = playerId) then
                let newPlayers = this.Players |> Set.filter (fun p -> p.Id <> playerId)
                Ok { this with Players = newPlayers }
            else
                Error NotInMatchmaking
        | _ -> Error(InvalidStatus this.Status)

    member this.End() : Result<(Matchmaking * bool), MatchmakingError> =
        match this.Status with
        | Running ->
            let cnt = this.Players.Count
            let min = Settings.MinPlayers.value this.Settings.MinPlayers

            if cnt >= min then
                Ok({ this with Status = Ended Succeeded }, true)
            else
                Ok(
                    { this with
                        Status = Ended NotEnoughPlayers },
                    false
                )
        | _ -> Error(InvalidStatus this.Status)

    member this.Fail reason =
        match this.Status with
        | Running -> Ok({ this with Status = Failed reason })
        | _ -> Error(InvalidStatus this.Status)
    
    member this.HasSucceeded =
        match this.Status with
        | Ended Succeeded -> true
        | _ -> false
