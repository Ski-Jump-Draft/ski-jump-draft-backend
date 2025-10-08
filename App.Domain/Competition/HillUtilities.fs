namespace App.Domain.Competition

module HillWindPointsCalculator =
    let calculateFisTailwindPoints (headwindPoints: Hill.WindPoints) =
        let headwindPointsDouble = Hill.WindPoints.value headwindPoints
        (Hill.WindPoints.tryCreate (headwindPointsDouble * 1.5)).Value

module HillPointsForMeterCalculator =
    let calculate (k: double) =
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

module HillPointsPerKCalculator =
    let calculate (k: double) : double =
        if k >= 200.0 then 120.0 else 60.0