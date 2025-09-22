namespace App.Domain.Simulation

open System

type Distance = private Distance of double

module Distance =
    let tryCreate (v: float) : Distance option =
        if v > 0.0 then
            let rounded = Math.Round(v * 2.0, MidpointRounding.AwayFromZero) / 2.0
            Some(Distance rounded)
        else
            None

    let value (Distance v) = v

type Landing =
    | Telemark
    | Parallel
    | TouchDown
    | Fall

type WindInstability = private WindInstability of double

module WindInstability =
    let clamp minVal maxVal (v: float) =
        if v < minVal then minVal
        elif v > maxVal then maxVal
        else v

    let create (v: double) =
        let instability = clamp v 0.0 1.0
        WindInstability instability

    let value (WindInstability v) = v

type Wind =
    private
        { Average: double
          Instability: WindInstability }

module Wind =
    let create (averaged: double, instability: WindInstability) =
        { Average = averaged
          Instability = instability }

    let average (v: Wind) = v.Average
    let instability (v: Wind) : double = (WindInstability.value v.Instability)

type Jump =
    { Distance: Distance; Landing: Landing }
