namespace App.Domain.Competition

open App.Domain.Competition.Hill

module ToBeatLineCalculator =
    let calculateToBeatDistance
        (hill: Hill)
        (gateState: GateState)
        (currentTotalPoints: double)
        (leaderTotalPoints: double)
        (currentWindAverage: double)
        (targetJudgePoints: double)
        : Jump.Distance =
        let k = KPoint.value hill.KPoint
        let hillGatePoints = GatePoints.value hill.GatePoints
        let hillHeadwindPoints = WindPoints.value hill.HeadwindPoints
        let hillTailwindPoints = WindPoints.value hill.TailwindPoints

        let windPoints =
            if currentWindAverage < 0 then
                -currentWindAverage * hillTailwindPoints
            else
                -currentWindAverage * hillHeadwindPoints

        let startingGate = Gate.value gateState.Starting
        let currentGate = Gate.value gateState.CurrentReal
        let gateCompensationBehindLeader = (double)(currentGate - startingGate) * hillGatePoints

        let totalCompensatedPoints =
            leaderTotalPoints
            - targetJudgePoints
            - windPoints
            - gateCompensationBehindLeader
            - currentTotalPoints

        let pointsPerMeter = HillPointsForMeterCalculator.calculate (k)
        
        let pointsPerK = HillPointsPerKCalculator.calculate k
        
        let toBeatDistance = k + ((totalCompensatedPoints - pointsPerK) / pointsPerMeter)
        
        Jump.Distance.tryCreate toBeatDistance |> Result.toOption |> Option.get