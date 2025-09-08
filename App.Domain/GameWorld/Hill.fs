namespace App.Domain.GameWorld

open System.Collections.Generic
open System.Threading
open System.Threading.Tasks

type HillId = HillId of System.Guid

module Hill =
    type Name = Name of string
    type Location = Location of string

    type KPoint = private KPoint of int

    module KPoint =
        let tryCreate (v: int) =
            if v >= 0 then Some(KPoint v) else Option.None

        let value (KPoint v) = v

    type HsPoint = private HsPoint of int

    module HsPoint =
        let tryCreate (v: int) =
            if v >= 0 then Some(HsPoint v) else Option.None

        let value (HsPoint v) = v

    type GatePoints = private GatePoints of double

    module GatePoints =
        let tryCreate (v: double) =
            if v > 0 then Some(GatePoints v) else Option.None

        let value (GatePoints v) = v

    type WindPoints = private WindPoints of double

    module WindPoints =
        let tryCreate (v: double) =
            if v > 0 then Some(WindPoints v) else Option.None

        let value (WindPoints v) = v

open Hill

type HillError =
    | HsNotGreaterThanK
    | HeadwindPointsGreaterThanTailwindPoints

type Hill =
    { Id: HillId
      Name: Name
      Location: Location
      CountryId: CountryFisCode
      KPoint: KPoint
      HsPoint: HsPoint
      GatePoints: GatePoints
      HeadwindPoints: WindPoints
      TailwindPoints: WindPoints }

    static member Create id name location countryId kPoint hsPoint gatePoints headwindPoints tailwindPoints =
        if (WindPoints.value headwindPoints > WindPoints.value tailwindPoints) then
            Error(HillError.HeadwindPointsGreaterThanTailwindPoints)
        elif (KPoint.value kPoint >= HsPoint.value hsPoint) then
            Error(HillError.HsNotGreaterThanK)
        else
            Ok(
                { Id = id
                  Name = name
                  Location = location
                  CountryId = countryId
                  KPoint = kPoint
                  HsPoint = hsPoint
                  GatePoints = gatePoints
                  HeadwindPoints = headwindPoints
                  TailwindPoints = tailwindPoints }
            )

type SearchFormattedName = private SearchFormattedName of string
module SearchFormattedName =
    let pattern = System.Text.RegularExpressions.Regex(@"^.+ HS\d+$")

    let tryCreate (v: string) =
        if pattern.IsMatch(v) then Some (SearchFormattedName v)
        else None

    let value (SearchFormattedName v) = v
        

type IHills =
    abstract member GetAll: ct: CancellationToken -> Task<IEnumerable<Hill>>
    abstract member GetByFormattedName: searchFormattedName: SearchFormattedName * ct: CancellationToken -> Task<Hill option>
    abstract member GetById: hillId: HillId * ct: CancellationToken -> Task<Hill option>
    abstract member GetByCountryFisCode: countryId: CountryFisCode * ct: CancellationToken -> Task<IEnumerable<Hill>>
