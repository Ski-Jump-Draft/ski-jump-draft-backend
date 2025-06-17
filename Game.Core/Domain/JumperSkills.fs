namespace Game.Core.Domain

type JumperBigSkill = private JumperBigSkill of float
module JumperBigSkill =
    let tryCreate(v: float) =
        if v > 0 then
            Some(JumperBigSkill v)
        else
            None
    let value (JumperBigSkill s) = s
    
type JumperLandingSkill =
    | Terrible
    | Poor
    | SoPoor
    | Average
    | Good
    | VeryGood
    | Excellent

type JumperLiveForm = private JumperLiveForm of float
module JumperLiveForm =
    let tryCreate(v: int) =
        if v >= 0 && v <= 10 then
            Some(JumperLiveForm v)
        else
            None
    let value (JumperLiveForm s) = s

type JumperSkills =
    {
        Takeoff: JumperBigSkill
        Flight: JumperBigSkill
        Landing: JumperLandingSkill
        LiveForm: JumperLiveForm
    }