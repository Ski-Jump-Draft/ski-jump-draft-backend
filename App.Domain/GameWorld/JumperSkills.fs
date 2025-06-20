namespace App.Domain.GameWorld

open App.Domain.Shared.Utils.Range

module JumperSkills =
    type BigSkill = private BigSkill of float

    module BigSkill =
        type Error = OutsideRange of OutsideRangeError<float>

        let tryCreate (v: float) =
            if v >= 1 && v <= 20 then
                Ok(BigSkill v)
            else
                Error(
                    OutsideRange
                        { Min = Some 1
                          Max = Some 20
                          Current = v }
                )

        let value (BigSkill s) = s


    type LandingSkill = private LandingSkill of int

    module LandingSkill =
        type Error = OutsideRange of OutsideRangeError<int>

        let tryCreate (v: int) =
            if v >= -3 && v <= 3 then
                Ok(LandingSkill v)
            else
                Error(
                    OutsideRange
                        { Min = Some -3
                          Max = Some 3
                          Current = v }
                )

        let value (v: LandingSkill) = v

    type LiveForm = private LiveForm of float

    module LiveForm =
        type Error = OutsideRange of OutsideRangeError<float>

        let tryCreate (v: int) =
            if v >= 0 && v <= 10 then
                Ok(LiveForm v)
            else
                Error(
                    OutsideRange
                        { Min = Some 1
                          Max = Some 10
                          Current = v }
                )

        let value (LiveForm s) = s

open JumperSkills

type JumperSkills =
    { Takeoff: BigSkill
      Flight: BigSkill
      Landing: LandingSkill
      LiveForm: LiveForm }
