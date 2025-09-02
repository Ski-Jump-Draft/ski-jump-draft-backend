namespace App.Domain._2.Competition

module HillWindPointsCalculator =
    let calculateFisTailwindPoints (headwindPoints: Hill.WindPoints) =
        let headwindPointsDouble = Hill.WindPoints.value headwindPoints
        (Hill.WindPoints.tryCreate (headwindPointsDouble * 1.5)).Value

module JumpResultCreator =
    open Jump

    type FisError = TooLessJudgeNotes of Count: int * Minimum: int


    let private baseDistancePoints (k: double) = if k >= 200.0 then 120.0 else 60.0

    let private pointsForMeter (k: double) =
        if k >= 180.0 then 1.2
        elif k >= 135.0 then 1.6
        elif k >= 100.0 then 1.8
        elif k >= 80.0 then 2.0
        elif k >= 70.0 then 2.2
        elif k >= 60.0 then 2.4
        elif k >= 50.0 then 2.8
        elif k >= 45.0 then 3.2
        elif k >= 40.0 then 3.6
        elif k >= 30.0 then 4.0
        elif k >= 25.0 then 4.4
        else 4.8

    let createFisJumpResult
        (id: JumpResultId)
        (jump: Jump)
        (jumper: Jumper)
        (hill: Hill)
        (roundIndex: RoundIndex)
        (gate: Gate)
        (coachGateChange: GateChange option)
        (startingGate: Gate)
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
                let pointsPerGate = Hill.GatePoints.value hill.GatePoints
                let gateDiff = float (startingGate - gate)
                let baseGatePoints = gateDiff * pointsPerGate

                let k = Hill.KPoint.value hill.KPoint
                let hs = Hill.HsPoint.value hill.HsPoint
                let distance95 = 0.95 * hs
                let baseDistance = Distance.value jump.Distance

                let distancePoints =
                    baseDistancePoints (k) + ((baseDistance - k) * pointsForMeter (k))

                let coachReduction =
                    match coachGateChange with
                    | Some coachGateChange ->
                        match coachGateChange with
                        | Reduction by -> by
                        | Increase _ ->
                            invalidOp "Coach cannot increase the gate. It should not happen, please report the bug."
                    | None -> 0u

                let coachPoints =
                    if coachReduction > 0u && baseDistance >= distance95 then
                        float coachReduction * pointsPerGate
                    else
                        0.0

                let totalGatePoints = baseGatePoints + coachPoints

                let windPoints =
                    match jump.Wind with
                    | WindAverage.Headwind headwind ->
                        let hillHeadwindPoints = Hill.WindPoints.value hill.HeadwindPoints
                        headwind * hillHeadwindPoints
                    | WindAverage.Tailwind tailwind ->
                        let hillTailwindPoints = Hill.WindPoints.value hill.TailwindPoints
                        tailwind * hillTailwindPoints
                    | WindAverage.Zero -> 0.0

                let totalPoints = distancePoints + judgePointsSum + totalGatePoints + windPoints

                Ok
                    { Id = id
                      JumperId = jumper.Id
                      Jump = jump
                      RoundIndex = roundIndex
                      JudgePoints = Some judgePoints
                      GatePoints = Some(JumpResult.GatePoints totalGatePoints)
                      WindPoints = Some(JumpResult.WindPoints windPoints)
                      TotalPoints = TotalPoints totalPoints }
