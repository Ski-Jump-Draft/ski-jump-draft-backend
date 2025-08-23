namespace App.Domain._2.Matchmaking

module Settings =
    type MinPlayers = private MinPlayers of int

    module MinPlayers =
        let create (v: int) =
            if v < 2 then None else Some(MinPlayers v)

        let value (MinPlayers v) = v

    type MaxPlayers = private MaxPlayers of int

    module MaxPlayers =
        let create (v: int) =
            if v < 2 then None else Some(MaxPlayers v)

        let value (MaxPlayers v) = v

type Settings =
    private
        { MinPlayers: Settings.MinPlayers
          MaxPlayers: Settings.MaxPlayers }

    static member Create minPlayers maxPlayers =
        if (Settings.MaxPlayers.value maxPlayers) < (Settings.MinPlayers.value minPlayers) then
            Error("MaxPlayers must be greater than or equal MinPlayers")
        else
            Ok(
                { MinPlayers = minPlayers
                  MaxPlayers = maxPlayers }
            )
