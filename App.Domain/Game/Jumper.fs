namespace App.Domain.Game

type JumperId = JumperId of System.Guid

[<CustomEquality; NoComparison>]
type Jumper =
    { Id: JumperId }
    
    override this.Equals(obj) =
        match obj with
        | :? Jumper as other -> this.Id = other.Id
        | _ -> false
    override this.GetHashCode() =
        hash this.Id

type Jumpers = private Jumpers of Map<JumperId, Jumper>

module Jumpers =
    let empty : Jumpers = Jumpers Map.empty

    let create (jumpers: Jumper list) : Jumpers =
        jumpers
        |> List.fold (fun acc j -> acc |> Map.add j.Id j) Map.empty
        |> Jumpers

    let add (jumper: Jumper) (Jumpers map) : Jumpers =
        Jumpers (Map.add jumper.Id jumper map)

    let toList (Jumpers map) : Jumper list =
        map |> Map.toList |> List.map snd
    
    let toIdsList (Jumpers map) : JumperId list =
        map |> Map.toList |> List.map snd |> List.map(_.Id)