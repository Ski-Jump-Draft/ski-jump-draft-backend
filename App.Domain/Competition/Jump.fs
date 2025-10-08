namespace App.Domain.Competition

open System

module Jump =
    type Id = Id of System.Guid

    type Distance = private Distance of double

    module Distance =
        type Error = BelowZero of Value: double

        let private roundToHalf v = (v * 2.0 |> round) / 2.0

        let tryCreate (v: double) =
            let v' = roundToHalf v
            if v' > 0.0 then Ok(Distance v') else Error(BelowZero v')

        let value (Distance v) = v

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

open Jump

type Jump =
    { JumperId: JumperId
      Distance: Distance
      JudgeNotes: Judges
      Wind: WindAverage
      Gate: Gate }
