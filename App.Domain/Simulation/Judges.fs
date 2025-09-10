namespace App.Domain.Simulation

open System

type Judges = private Judges of double list
module Judges =
    let private roundToHalf (x: double) =
        Math.Round(x * 2.0, MidpointRounding.AwayFromZero) / 2.0

    let tryCreate (scores: double list) =
        if scores.Length = 5 then
            let rounded = scores |> List.map roundToHalf
            Some(Judges rounded)
        else
            None

    let value (Judges scores) = scores
