namespace App.Domain.Competition.Results.ResultObjects

open System
open App.Domain.Competition
open App.Domain.Competition.Jump.Judgement
open App.Domain.Competition.Phase

module JumpScore =
    type Id = Id of Guid
    // type Points = Points of double

    type TotalPoints = TotalPoints of double

    type StylePoints =
        | None
        | CustomValue of Points: double
        | SumOfSelectedMarks of Marks: JudgeMarksList * Points: double

    type GatePoints =
        | None
        | Some of double

    type WindPoints =
        | None
        | Some of double

type JumpScore =
    { Points: JumpScore.TotalPoints
      StylePoints: JumpScore.StylePoints
      GatePoints: JumpScore.GatePoints
      WindPoints: JumpScore.WindPoints }

module JumpResult =
    type Id = Id of Guid

type JumpResult =
    { Id: JumpResult.Id
      IndividualParticipantId: IndividualParticipant.Id
      RoundIndex: RoundIndex
      Score: JumpScore }

type IndividualResult =
    { IndividualId: IndividualParticipant.Id
      JumpResults: JumpResult list }

type TeamResult =
    { TeamId: Team.Id
      MemberResults: IndividualResult list }

    member this.IndividualResultOf id =
        this.MemberResults |> List.tryFind (fun ir -> ir.IndividualId = id)

    member this.ContainsIndividualResult id =
        this.MemberResults |> List.exists (fun ir -> ir.IndividualId = id)

module ParticipantResult =
    type Id = Id of System.Guid
    type TotalPoints = TotalPoints of double

    [<AutoOpen>]
    module TotalPoints =
        let inline (+) (TotalPoints a) (TotalPoints b) = TotalPoints(a + b)
        let zero = TotalPoints 0

    type Details =
        | IndividualResultDetails of IndividualResult
        | TeamResultDetails of TeamResult

[<CustomEquality; NoComparison>]
type ParticipantResult =
    { Id: ParticipantResult.Id
      TotalPoints: Map<RoundIndex, ParticipantResult.TotalPoints>
      Details: ParticipantResult.Details }

    override this.Equals(obj) =
        match obj with
        | :? ParticipantResult as other -> this.Id = other.Id
        | _ -> false

    override this.GetHashCode() = hash this.Id

// type ParticipantResultDetailsId =
//     | IndividualId of IndividualParticipant.Id
//     | TeamId of Team.Id
