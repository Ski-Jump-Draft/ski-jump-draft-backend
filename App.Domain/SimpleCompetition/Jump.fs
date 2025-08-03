namespace App.Domain.SimpleCompetition

module Jump =
    type Id = Id of System.Guid

    type Distance = private Distance of double

    module Distance =
        type Error = BelowZero of Value: double

        let tryCreate (v: double) =
            if v > 0 then Ok(Distance v) else Error(Error.BelowZero v)

        let value (Distance v) = v

    type JudgeNotes = private JudgeNotes of double list

    module JudgeNotes =
        type Error = Empty

        let tryCreate (notes: double list) =
            if notes.IsEmpty then
                Error(Error.Empty)
            else
                Ok(JudgeNotes notes)

        let value (JudgeNotes v) = v

    type Gate = Gate of int

    module GateModule =
        let value (Gate v) = v

    type GatesLoweredByCoach = GatesLoweredByCoach of int

    module GatesLoweredByCoachModule =
        let value (GatesLoweredByCoach v) = v

    type WindAverage =
        | Headwind of Value: double
        | Tailwind of Value: double
        | Zero

        member this.ToDouble(): double =
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

type Jump =
    { Id: Jump.Id
      CompetitorId: Competitor.Id
      Distance: Jump.Distance
      JudgeNotes: Jump.JudgeNotes
      Gate: Jump.Gate
      GatesLoweredByCoach: Jump.GatesLoweredByCoach
      WindAverage: Jump.WindAverage }
