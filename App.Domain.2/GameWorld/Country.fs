namespace App.Domain._2.GameWorld

open System.Collections.Generic
open System.Threading
open System.Threading.Tasks

type CountryId = CountryId of System.Guid

type Alpha2Code = private Alpha2Code of string

module Alpha2Code =
    let tryCreate (v: string) =
        if System.String.IsNullOrWhiteSpace v then None
        elif v.Length = 2 then Some(Alpha2Code(v.ToUpper()))
        else None

    let value (Alpha2Code v) = v

type Alpha3Code = private Alpha3Code of string

module Alpha3Code =
    let tryCreate (v: string) =
        if System.String.IsNullOrWhiteSpace v then None
        elif v.Length = 3 then Some(Alpha3Code(v.ToUpper()))
        else None

    let value (Alpha3Code v) = v

type FisCode = private FisCode of string

module FisCode =
    let tryCreate (v: string) =
        if System.String.IsNullOrWhiteSpace v then None
        elif v.Length = 3 then Some(FisCode(v.ToUpper()))
        else None

    let value (FisCode v) = v


type Country =
    { Id: CountryId
      Alpha2: Alpha2Code
      Alpha3: Alpha3Code
      FisCode: FisCode }

type ICountries =
    abstract member GetById: countryId: CountryId * ct: CancellationToken -> Task<Country option>
    abstract member GetAll: countryId: CountryId * ct: CancellationToken -> Task<IEnumerable<Country>>
    abstract member GetByFisCode: fisCode: FisCode * ct: CancellationToken -> Task<Country option>
