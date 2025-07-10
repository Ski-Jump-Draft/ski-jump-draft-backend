module App.Domain.Competition.Results.Abstractions

open App.Domain.Competition
open App.Domain.Competition.Jump
open App.Domain.Competition.Phase
open App.Domain.Competition.Results.ResultObjects
open App.Domain.Competition.Results.ResultObjects.JumpScore

type IGatePointsGrantor =
    abstract Grant: Jump: Jump -> GatePoints

type IWindPointsGrantor =
    abstract Grant: Jump: Jump -> WindPoints

type IStylePointsAggregator =
    abstract Aggregate: JudgeMarks: Judgement.JudgeMarksList -> StylePoints

type IJumpScorer =
    abstract Evaluate: Jump: Jump -> JumpScore

type IJumpResultCreator =
    abstract Create:
        Score: JumpScore * RoundIndex: RoundIndex * IndividualParticipantId: IndividualParticipant.Id -> JumpResult
