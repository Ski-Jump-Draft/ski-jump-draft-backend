module App.Domain.Competition.Jump.Abstractions

open App.Domain.Competition
open App.Domain.Competition.Jump.Wind

type IGateStatusProvider =
    abstract member Provide: Jump.Jump -> Gate.GateStatus

type IWindMeasurementProvider =
    abstract member GetMeasurements: Jump.Jump -> Wind.WindMeasurement

type IWindAggregator =
    abstract member Aggregate: Wind.WindMeasurement -> AggregatedWind