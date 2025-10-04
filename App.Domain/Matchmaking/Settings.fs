namespace App.Domain.Matchmaking

open System

module Settings =
    type Duration = Duration of TimeSpan

    type MatchmakingEndPolicy =
        | AfterNoUpdate of Since: TimeSpan
        | AfterReachingMaxPlayers of After: TimeSpan
        | AfterReachingMinPlayers of After: TimeSpan
        | AfterTimeout

    type MinPlayers = private MinPlayers of int

    module MinPlayers =
        let create (v: int) : MinPlayers option =
            if v < 2 then None else Some(MinPlayers v)

        let value (MinPlayers v) = v

    type MaxPlayers = private MaxPlayers of int

    module MaxPlayers =
        let create (v: int) : MaxPlayers option =
            if v < 2 then None else Some(MaxPlayers v)

        let value (MaxPlayers v) = v

type Settings =
    private
        { MaxDuration: Settings.Duration
          MatchmakingEndPolicy: Settings.MatchmakingEndPolicy
          MinPlayers: Settings.MinPlayers
          MaxPlayers: Settings.MaxPlayers }

    static member Create maxDuration autoStartPolicy minPlayers maxPlayers =
        if (Settings.MaxPlayers.value maxPlayers) < (Settings.MinPlayers.value minPlayers) then
            Error("MaxPlayers must be greater than or equal MinPlayers")
        else
            Ok(
                { MaxDuration = maxDuration
                  MatchmakingEndPolicy = autoStartPolicy
                  MinPlayers = minPlayers
                  MaxPlayers = maxPlayers }
            )
