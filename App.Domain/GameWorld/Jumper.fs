namespace App.Domain.GameWorld

open System.Collections.Generic
open System.Threading
open System.Threading.Tasks

type JumperId=  JumperId of System.Guid

module Jumper =
    type Name = Name of string
    type Surname = Surname of string
    
    let private inRange (minv: 'a) (maxv: 'a) (v: 'a) : bool when 'a : comparison =
        v >= minv && v <= maxv

    type BigSkill = private BigSkill of double

    module BigSkill =
        let tryCreate (v: double) : BigSkill option =
            if inRange 0.0 10.0 v then Some (BigSkill v) else None

        let value (BigSkill s) = s

    type LandingSkill = private LandingSkill of int

    module LandingSkill =
        let tryCreate (v: int) : LandingSkill option =
            if inRange 1 10 v then Some (LandingSkill v) else None

        let value (LandingSkill s) = s

    type LiveForm = private LiveForm of int

    module LiveForm =
        let tryCreate (v: int) : LiveForm option =
            if inRange 0 10 v then Some (LiveForm v) else None

        let value (LiveForm s) = s

open Jumper

type Jumper = {
    Id: JumperId
    Name: Name
    Surname: Surname
    CountryId: CountryId
    Takeoff: BigSkill
    Flight: BigSkill
    Landing: LandingSkill
    LiveForm: LiveForm
}

type IJumpers =
    abstract member GetAll : ct: CancellationToken -> Task<IEnumerable<Jumper>>
    abstract member GetById: jumperId: JumperId  * ct: CancellationToken -> Task<Jumper option>
    abstract member GetByCountryId: countryId: CountryId  * ct: CancellationToken -> Task<IEnumerable<Jumper>>