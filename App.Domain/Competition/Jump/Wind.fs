module App.Domain.Competition.Jump.Wind

module SingleWindMeasurement =
    type StrengthMs = StrengthMs of double
    type DirectionAngle = private DirectionAngle of double

    module DirectionAngle =
        type Error = NotInRange of Value: double

        let tryCreate (v: double) =
            if v >= 0 && v <= 360 then
                Ok(DirectionAngle v)
            else
                Error(Error.NotInRange v)

type SingleWindMeasurement =
    { StrengthMs: SingleWindMeasurement.StrengthMs
      DirectionAngle: SingleWindMeasurement.DirectionAngle }

module WindSegment =
    type Index = Index of uint
    type RangeEnd = RangeEnd of double

type WindSegment =
    { Index: WindSegment.Index
      From: WindSegment.RangeEnd
      To: WindSegment.RangeEnd
      Measurement: SingleWindMeasurement }

type WindMeasurement =
    | None
    | Single of SingleWindMeasurement
    | Multiple of WindSegment list

type AggregatedWind =
    | AggregatedWind of double

    member this.IsZero = this = AggregatedWind(0)
    member this.IsHeadwind = this > AggregatedWind(0)
    member this.IsTailwind = this < AggregatedWind(0)
