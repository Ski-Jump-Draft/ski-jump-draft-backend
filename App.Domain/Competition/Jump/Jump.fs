namespace App.Domain.Competition.Jump

open App.Domain.Competition
open App.Domain.Competition.Jump

type Distance = private Distance of double

module Distance =
    type Error = DistanceZeroOrLess of Distance: double

    let tryCreate (v: double) =
        if v >= 0 then
            Ok(Distance v)
        else
            Error(Error.DistanceZeroOrLess v)

    let value (Distance v) = v

module Jump =
    type Id = Id of System.Guid

type Jump =
    { Id: Jump.Id
      IndividualParticipantId: IndividualParticipant.Id
      HillId: Hill.Id
      GateStatus: Gate.GateStatus
      WindMeasurement: Wind.WindMeasurement
      JudgeMarks: Judgement.JudgeMarksList option
      Distance: Distance
      KPoint: Hill.KPoint
      HSPoint: Hill.HSPoint }
