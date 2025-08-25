namespace App.Domain._2.Game

module Ranking =
    type Points = private Points of int
    module Points =
        let create (v: int) =
            if v < 0 then None
            else Some(Points v)
        let value (Points v) = v
            
    type Position = private Position of int
    module Position =
        let create (v: int) =
            if v >= 1 then Some (Position v) else None
        let value (Position v) = v

type Ranking = private Ranking of Map<PlayerId, Ranking.Points>
with
    static member Create (ranking: Map<PlayerId, Ranking.Points>) =
        Ranking ranking

    member this.AllPositions : Map<PlayerId, Ranking.Position> =
        let (Ranking playerPoints) = this

        let playersSortedByPoints =
            playerPoints
            |> Map.toList
            |> List.sortByDescending (fun (_, points) -> Ranking.Points.value points)

        let rec assignPositions positions assignedPoints lastPosition nextFreePosition remainingPlayers =
            match remainingPlayers with
            | [] -> positions
            | (playerId, points)::rest ->
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

    member this.PositionOf(playerId: PlayerId) : Ranking.Position option =
        this.AllPositions |> Map.tryFind playerId
        
    member this.PointsOf(playerId: PlayerId) : Ranking.Points option =
        let (Ranking map) = this
        map.TryFind playerId