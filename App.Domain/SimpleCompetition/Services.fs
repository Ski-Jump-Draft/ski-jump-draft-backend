namespace App.Domain.SimpleCompetition

open App.Domain.SimpleCompetition
open App.Domain.SimpleCompetition.Jump
open App.Domain.SimpleCompetition.Hill

module HillWindPointsCalculator =
    let calculateFisTailwindPoints (headwindPoints: Hill.WindPoints) =
        let headwindPointsDouble = WindPoints.value headwindPoints
        (WindPoints.tryCreate (headwindPointsDouble * 1.5)).Value

module JumpResultCreator =
    type FisError = TooLessJudgeNotes of Count: int * Minimum: int

    let createFisJumpResult
        (id: JumpResult.Id)
        (jump: Jump)
        (competitor: Competitor)
        (hill: Hill)
        (roundIndex: RoundIndex)
        (groupIndex: GroupIndex option)
        (gate: Jump.Gate)
        (coachGateChange: GateChange option)
        (startingGate: Jump.Gate)
        : Result<JumpResult, FisError> =

        let judgeNotes = JudgeNotes.value jump.JudgeNotes
        let judgeNotesCount = judgeNotes.Length

        if judgeNotesCount < 3 then
            Error(FisError.TooLessJudgeNotes(judgeNotesCount, 3))
        else
            let judgePointsSum =
                judgeNotes
                |> List.sort
                |> List.tail
                |> List.take (judgeNotesCount - 2)
                |> List.sum

            match JumpResult.JudgePoints.tryCreate judgePointsSum with
            | Error _ -> failwith "Invalid judge points"
            | Ok judgePoints ->
                let (Gate gate) = gate
                let (Gate startingGate) = startingGate
                let pointsPerGate = GatePoints.value hill.GatePoints
                let gateDiff = float (startingGate - gate)
                let baseGatePoints = gateDiff * pointsPerGate

                let hs = HsPoint.value hill.HsPoint
                let distance95 = 0.95 * hs
                let baseDistance = Distance.value jump.Distance

                let coachReduction =
                    match coachGateChange with
                    | Some coachGateChange ->
                        match coachGateChange with
                        | Reduction by -> by
                        | Increase _ ->
                            invalidOp "Coach cannot increase the gate. It should not happen, please report the bug."
                    | None ->
                        0u 

                let coachPoints =
                    if coachReduction > 0u && baseDistance >= distance95 then
                        float coachReduction * pointsPerGate
                    else
                        0.0

                let totalGatePoints = baseGatePoints + coachPoints

                let windPoints =
                    match jump.WindAverage with
                    | WindAverage.Headwind v ->
                        let hillHeadwindPoints = WindPoints.value hill.HeadwindPoints
                        v * hillHeadwindPoints
                    | WindAverage.Tailwind v ->
                        let hillTailwindPoints = WindPoints.value hill.TailwindPoints
                        v * hillTailwindPoints
                    | WindAverage.Zero -> 0.0

                let totalPoints = judgePointsSum + totalGatePoints + windPoints

                Ok
                    { Id = id
                      Jump = jump
                      CompetitorId = competitor.Id
                      TeamId = competitor.TeamId
                      RoundIndex = roundIndex
                      GroupIndex = groupIndex
                      JudgePoints = Some judgePoints
                      GatePoints = Some(JumpResult.GatePoints totalGatePoints)
                      WindPoints = Some(JumpResult.WindPoints windPoints)
                      TotalPoints = JumpResult.TotalPoints totalPoints }
