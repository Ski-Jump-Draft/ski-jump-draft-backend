namespace App.Domain.Simulation

module JumperSkills =
    let private inRange (minv: 'a) (maxv: 'a) (v: 'a) : bool when 'a : comparison =
        v >= minv && v <= maxv

    type BigSkill = private BigSkill of double

    module BigSkill =
        let tryCreate (v: double) : BigSkill option =
            if inRange 1.0 20.0 v then Some (BigSkill v) else None

        let value (BigSkill s) = s

    type LandingSkill = private LandingSkill of int

    module LandingSkill =
        let tryCreate (v: int) : LandingSkill option =
            if inRange 1 10 v then Some (LandingSkill v) else None

        let value (LandingSkill s) = s

    type Form = private LiveForm of double

    module Form =
        let tryCreate (v: double) : Form option =
            if inRange 0.0 10.0 v then Some (LiveForm v) else None

        let value (LiveForm s) = s
        
    type LikesHillPolicy =
        | DoNotLike
        | None
        | Likes

open JumperSkills

type JumperSkills =
    { Takeoff: BigSkill
      Flight: BigSkill
      Landing: LandingSkill
      Form: Form
      LikesHill: LikesHillPolicy}

type Jumper = {
    Skills: JumperSkills
}

