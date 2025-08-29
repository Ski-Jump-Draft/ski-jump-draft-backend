namespace App.Domain._2.Simulation

open System

type Distance = private Distance of double
module Distance =
    let tryCreate (v: float) : Distance option =
        if v > 0.0 then
            let rounded = Math.Round(v * 2.0, MidpointRounding.AwayFromZero) / 2.0
            Some (Distance rounded)
        else
            None

    let value (Distance v) = v

type Landing =
    | Telemark
    | Parallel
    | TouchDown
    | Fall
    
type Wind = private {
    Average: double
}
module Wind =
    let create (averaged: double) =
        { Average = averaged }
    let averaged (v: Wind) = v.Average

type Jump = {
    Distance: Distance
    Landing: Landing
}

