namespace App.Domain.Matchmaking

open System
open System.Collections.Generic
open App.Domain.Matchmaking.Settings

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
    | InvalidJoinedAt

type Matchmaking =
    private
        { Id: MatchmakingId
          IsPremium: bool
          Settings: Settings
          Status: Status
          Players: Set<Player>
          StartedAt: DateTimeOffset
          EndedAt: DateTimeOffset option
          ReachedMaxPlayersAt: DateTimeOffset option
          ReachedMinPlayersAt: DateTimeOffset option
          LastUpdatedAt: DateTimeOffset option }

    static member CreateNew id premium settings now =
        { Id = id
          IsPremium = premium
          Settings = settings
          Status = Running
          Players = Set.empty
          StartedAt = now
          EndedAt = None
          ReachedMaxPlayersAt = None
          ReachedMinPlayersAt = None
          LastUpdatedAt = None }

    static member CreateFromState
        id
        isPremium
        settings
        status
        players
        startedAt
        endedAt
        reachedMaxPlayersAt
        reachedMinPlayersAt
        lastUpdatedAt
        =
        { Id = id
          IsPremium = isPremium
          Settings = settings
          Status = status
          Players = players
          StartedAt = startedAt
          EndedAt = endedAt
          ReachedMaxPlayersAt = reachedMaxPlayersAt
          ReachedMinPlayersAt = reachedMinPlayersAt
          LastUpdatedAt = lastUpdatedAt }

    member this.Id_: MatchmakingId = this.Id
    member this.Status_: Status = this.Status
    member this.Players_: IReadOnlyCollection<Player> = this.Players
    member this.IsPremium_ : bool = this.IsPremium
    member this.PlayersCount = this.Players.Count
    member this.MinPlayersCount = this.Settings.MinPlayers
    member this.MaxPlayersCount = this.Settings.MaxPlayers

    member this.MaxDuration =
        let (Settings.Duration duration) = this.Settings.MaxDuration
        duration

    member this.StartedAt_ = this.StartedAt
    member this.EndedAt_: DateTimeOffset option = this.EndedAt
    member this.ReachedMaxPlayersAt_: DateTimeOffset option = this.ReachedMaxPlayersAt
    member this.ReachedMinPlayersAt_: DateTimeOffset option = this.ReachedMinPlayersAt
    member this.LastUpdatedAt_: DateTimeOffset option = this.LastUpdatedAt
    member this.EndPolicy = this.Settings.MatchmakingEndPolicy

    member this.MinRequiredPlayers =
        let currentPlayers = this.PlayersCount
        let minPlayers = Settings.MinPlayers.value this.Settings.MinPlayers

        if currentPlayers < minPlayers then
            Some(minPlayers - currentPlayers)
        else
            None

    member this.Join (player: Player) (now: DateTimeOffset) : Result<Matchmaking * Player.Nick, MatchmakingError> =
        if player.JoinedAt <> now then
            Error(MatchmakingError.InvalidJoinedAt)
        else
            match this.Status with
            | Running ->
                if this.Players |> Set.exists (fun p -> p.Id = player.Id) then
                    Error AlreadyJoined
                elif this.Players.Count >= Settings.MaxPlayers.value this.Settings.MaxPlayers then
                    Error TooManyPlayers
                else
                    let newPlayersCount = this.PlayersCount + 1

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

                    let reachedMaxAt =
                        if this.ReachedMaxPlayers newPlayersCount then
                            Some now
                        else
                            None

                    let reachedMinAt =
                        if this.ReachedMinPlayers newPlayersCount then
                            Some now
                        else
                            None

                    Ok(
                        { this with
                            Players = this.Players.Add { player with Nick = nick' }
                            LastUpdatedAt = Some now
                            ReachedMaxPlayersAt = reachedMaxAt
                            ReachedMinPlayersAt = reachedMinAt },
                        nick'
                    )
            | _ -> Error(InvalidStatus this.Status)

    member this.CanJoin: bool =
        match this.Status with
        | Ended _ -> false
        | Failed _ -> false
        | Running -> not (this.Players.Count >= Settings.MaxPlayers.value this.Settings.MaxPlayers)

    member this.ReachedMaxPlayers(count: int) =
        count >= MaxPlayers.value this.Settings.MaxPlayers

    member this.ReachedMinPlayers(count: int) =
        count >= MinPlayers.value this.Settings.MinPlayers

    member this.IsFull: bool = this.RemainingSlots = 0

    member this.RemainingSlots: int =
        MaxPlayers.value this.MaxPlayersCount - this.PlayersCount

    member this.Leave playerId now =
        match this.Status with
        | Running ->
            if this.Players |> Set.exists (fun p -> p.Id = playerId) then
                let newPlayers = this.Players |> Set.filter (fun p -> p.Id <> playerId)

                let newPlayersCount = this.PlayersCount - 1

                let reachedMaxAt =
                    if this.ReachedMaxPlayers newPlayersCount then
                        Some now
                    else
                        None

                let reachedMinAt =
                    if this.ReachedMinPlayers newPlayersCount then
                        Some now
                    else
                        None

                Ok
                    { this with
                        Players = newPlayers
                        LastUpdatedAt = Some now
                        ReachedMaxPlayersAt = reachedMaxAt
                        ReachedMinPlayersAt = reachedMinAt }
            else
                Error NotInMatchmaking
        | _ -> Error(InvalidStatus this.Status)

    member this.ForceEndAt(now: DateTimeOffset) : DateTimeOffset =
        let (Duration maxDuration) = this.Settings.MaxDuration
        this.StartedAt + maxDuration

    member this.RemainingToForceEnd(now: DateTimeOffset) : TimeSpan =
        let forceEndAt = this.ForceEndAt now
        let remainingToForceEnd = forceEndAt - now
        remainingToForceEnd

    member this.AcceleratedEndAt(now: DateTimeOffset) : DateTimeOffset option =
        match this.Settings.MatchmakingEndPolicy with
        | AfterTimeout -> None
        | AfterNoUpdate since -> this.LastUpdatedAt |> Option.map (fun lastUpdateAt -> lastUpdateAt + since)
        | AfterReachingMaxPlayers after ->
            match (this.ReachedMaxPlayers this.PlayersCount, this.ReachedMaxPlayersAt) with
            | true, Some reachedAt -> Some(reachedAt + after)
            | _ -> None
        | AfterReachingMinPlayers after ->
            match (this.ReachedMinPlayers this.PlayersCount, this.ReachedMinPlayersAt) with
            | true, Some reachedAt -> Some(reachedAt + after)
            | _ -> None

    member this.RemainingTimeFrom(now: DateTimeOffset) : TimeSpan =
        match this.RemainingTimeToAcceleratedEnd now with
        | Some timeToAcceleratedEnd -> timeToAcceleratedEnd
        | None -> this.RemainingToForceEnd now

    member this.RemainingTimeToAcceleratedEnd(now: DateTimeOffset) : TimeSpan option =
        match this.AcceleratedEndAt now with
        | None -> None
        | Some acceleratedEndAt ->
            let remaining = acceleratedEndAt - now
            Some remaining

    member this.ShouldEnd(now: DateTimeOffset) : bool =
        this.RemainingTimeFrom now <= TimeSpan.Zero

    member this.End now : Result<(Matchmaking * bool), MatchmakingError> =
        match this.Status with
        | Running ->
            let playersCount = this.Players.Count
            let minPlayersCount = Settings.MinPlayers.value this.Settings.MinPlayers

            if playersCount >= minPlayersCount then
                Ok(
                    { this with
                        Status = Ended Succeeded
                        EndedAt = now },
                    true
                )
            else
                Ok(
                    { this with
                        Status = Ended NotEnoughPlayers
                        EndedAt = now },
                    false
                )
        | _ -> Error(InvalidStatus this.Status)

    member this.Fail reason now =
        match this.Status with
        | Running ->
            Ok(
                { this with
                    Status = Failed reason
                    EndedAt = now }
            )
        | _ -> Error(InvalidStatus this.Status)

    member this.HasSucceeded =
        match this.Status with
        | Ended Succeeded -> true
        | _ -> false
