namespace App.Domain._2.Competition

type RoundLimitValue = private RoundLimitValue of int

module RoundLimitValue =
    type Error = | BelowTwo

    let tryCreate (v: int) =
        if v < 2 then
            Error(Error.BelowTwo)
        else
            Ok(RoundLimitValue v)

type RoundLimit =
    | NoneLimit
    | Soft of Value: RoundLimitValue
    | Exact of Value: RoundLimitValue // Przechodzi najwyższy BIB (jeśli KO, to najniższy)

type RoundSettings =
    { RoundLimit: RoundLimit
      SortStartlist: bool
      ResetPoints: bool}

module Settings =
    type Error = | RoundSettingsEmpty

type Settings =
    private
        { RoundSettings: RoundSettings list }

    static member Create(roundSettings: RoundSettings list) =
        if roundSettings.IsEmpty then
            Error(Settings.Error.RoundSettingsEmpty)
        else
            Ok { RoundSettings = roundSettings }
    
    member this.PointsResets : RoundIndex list =
            this.RoundSettings
            |> List.mapi (fun i rs -> if rs.ResetPoints then Some (RoundIndex(uint i)) else None)
            |> List.choose id