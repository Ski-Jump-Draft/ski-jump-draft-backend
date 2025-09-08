namespace App.Domain.Game

module Ranking =
    type Points = private Points of int

    module Points =
        let create (v: int) = if v < 0 then None else Some(Points v)
        let value (Points v) = v

    type Position = private Position of int

    module Position =
        let create (v: int) =
            if v >= 1 then Some(Position v) else None

        let value (Position v) = v

type Ranking =
    private
    | Ranking of Map<PlayerId, Ranking.Points>

    static member Create(ranking: Map<PlayerId, Ranking.Points>) = Ranking ranking

    member this.AllPositions: Map<PlayerId, Ranking.Position> =
        let (Ranking playerPoints) = this

        let playersSortedByPoints =
            playerPoints
            |> Map.toList
            |> List.sortByDescending (fun (_, points) -> Ranking.Points.value points)

        let rec assignPositions positions assignedPoints lastPosition nextFreePosition remainingPlayers =
            match remainingPlayers with
            | [] -> positions
            | (playerId, points) :: rest ->
                let currentPoints = Ranking.Points.value points

                match assignedPoints, lastPosition with
                | Some prevPoints, Some prevPosition when prevPoints = currentPoints ->
                    let position = Ranking.Position.create prevPosition |> Option.get
                    let updated = Map.add playerId position positions
                    assignPositions updated assignedPoints lastPosition (nextFreePosition + 1) rest
                | _ ->
                    let position = Ranking.Position.create nextFreePosition |> Option.get
                    let updated = Map.add playerId position positions
                    assignPositions updated (Some currentPoints) (Some nextFreePosition) (nextFreePosition + 1) rest

        assignPositions Map.empty None None 1 playersSortedByPoints
        
    member this.PositionsAndPoints : Map<PlayerId, Ranking.Position * Ranking.Points> =
        let (Ranking playerPoints) = this
        this.AllPositions
        |> Map.map (fun playerId pos ->
            let points = playerPoints.[playerId]
            (pos, points)
        )

    member this.PositionOf(playerId: PlayerId) : Ranking.Position option =
        this.AllPositions |> Map.tryFind playerId

    member this.PointsOf(playerId: PlayerId) : Ranking.Points option =
        let (Ranking map) = this
        map.TryFind playerId

    member this.PrettyPrint(nicknames: Map<PlayerId, string>) =
        let positions = this.AllPositions

        positions
        |> Map.toList
        |> List.sortBy (fun (_, pos) -> Ranking.Position.value pos)
        |> List.map (fun (playerId, pos) ->
            let pts =
                this.PointsOf playerId
                |> Option.map Ranking.Points.value
                |> Option.defaultValue 0

            let name =
                nicknames |> Map.tryFind playerId |> Option.defaultValue (string playerId)

            $"#{Ranking.Position.value pos}: {name} ({pts} pts)")
        |> String.concat "\n"
