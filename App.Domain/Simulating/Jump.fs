namespace App.Domain.Simulating


type Distance = private Distance of double

module Distance =
    type Error = DistanceZeroOrLess of Distance: double

    let tryCreate (v: double) =
        if v >= 0 then
            Ok(Distance v)
        else
            Error(Error.DistanceZeroOrLess v)

type Gate = Gate of int

type WindAverage =
    | Headwind of Value: double
    | Tailwind of Value: double
    | Zero

    member this.ToDouble() : double =
        match this with
        | Headwind value -> value
        | Tailwind value -> -value
        | Zero -> 0

    static member CreateHeadwind(v: double) =
        if v <= 0.0 then
            failwith "Must be > 0"

        Headwind v

    static member CreateTailwind(v: double) =
        if v <= 0.0 then
            failwith "Must be > 0"

        Tailwind v

    static member FromDouble(v: double) =
        if v = 0 then Zero
        elif v <= 0.0 then WindAverage.CreateTailwind -v
        else WindAverage.CreateHeadwind v

type Landing =
    | Fall
    | Supported
    | BothLegs
    | Telemark

type Jump =
    { Gate: Gate
      WindAverage: WindAverage
      Distance: Distance
      Landing: Landing }
