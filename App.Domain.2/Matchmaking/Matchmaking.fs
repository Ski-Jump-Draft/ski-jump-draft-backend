namespace App.Domain._2.Matchmaking

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

type Matchmaking = private {
    Settings: Settings
    Status: Status
    Players: Set<Player>
} with
    static member Create settings =
        {
            Settings= settings
            Status = Running
            Players = Set.empty
        }
        
    member this.Join player =
        match this.Status with
        | Running ->
            if this.Players |> Set.exists (fun p -> p.Id = player.Id) then
                Error AlreadyJoined
            elif this.Players.Count >= Settings.MaxPlayers.value this.Settings.MaxPlayers then
                Error TooManyPlayers
            else
                Ok { this with Players = this.Players.Add player }
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
        
    member this.End =
        match this.Status with
        | Running ->
            let cnt = this.Players.Count
            let min = Settings.MinPlayers.value this.Settings.MinPlayers
            if cnt >= min then
                Ok { this with Status = Ended Succeeded }
            else
                Ok { this with Status = Ended NotEnoughPlayers }
        | _ -> Error(InvalidStatus this.Status)
        
    member this.Fail reason =
        match this.Status with
        | Running ->
            Ok({ this with Status = Failed  reason})
        | _ -> Error(InvalidStatus this.Status)

