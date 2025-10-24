namespace App.Domain.Game

open System.Text.RegularExpressions

type PlayerId = PlayerId of System.Guid

module Player =
    type Nick = private Nick of string

    module Nick =
        let suffixRegex = Regex(@"\(\d+\)$", RegexOptions.Compiled)

        let create (v: string) =
            if v.Contains("(") || v.Contains(")") then None
            else if v.Length <= 25 then Some(Nick v)
            else None

        let createWithSuffix (v: string) =
            if suffixRegex.IsMatch(v) then Some(Nick v) else create v

        let value (Nick v) = v

[<CustomEquality; NoComparison>]
type Player =
    { Id: PlayerId
      Nick: Player.Nick }

    override this.Equals(obj) =
        match obj with
        | :? Player as other -> this.Id = other.Id
        | _ -> false

    override this.GetHashCode() = hash this.Id

type Players = private Players of Map<PlayerId, Player>

module Players =
    let empty: Players = Players Map.empty

    let create (players: Player list) : Result<Players, string> =
        // sprawdzamy unikalność nicków
        let nickValues = players |> List.map (fun p -> Player.Nick.value p.Nick)

        let duplicateNick =
            nickValues
            |> List.groupBy id
            |> List.tryFind (fun (_, xs) -> List.length xs > 1)

        match duplicateNick with
        | Some(nick, _) -> Error $"Duplicate nick: {nick}"
        | None ->
            // budujemy mapę Id->Player, dzięki czemu ID będą unikalne
            let playersMap =
                players
                |> List.fold
                    (fun acc p ->
                        if acc |> Map.containsKey p.Id then
                            acc
                        else
                            acc |> Map.add p.Id p)
                    Map.empty

            Ok(Players playersMap)

    let add (player: Player) (Players map) : Result<Players, string> =
        // sprawdź nicki
        if
            map
            |> Map.exists (fun _ p -> Player.Nick.value p.Nick = Player.Nick.value player.Nick)
        then
            Error $"Duplicate nick: {Player.Nick.value player.Nick}"
        else if
            // jeśli ID już istnieje → pomijamy
            map |> Map.containsKey player.Id
        then
            Ok(Players map)
        else
            Ok(Players(Map.add player.Id player map))

    let toList (Players map) : Player list = map |> Map.toList |> List.map snd

    let toIdsList (Players map) : PlayerId list =
        map |> Map.toList |> List.map snd |> List.map (_.Id)

    let count (Players map) : int = map |> Map.count
