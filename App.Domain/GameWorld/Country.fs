namespace App.Domain.GameWorld

open System.Collections.Generic
open System.Threading
open System.Threading.Tasks

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

type CountryFisCode = private CountryFisCode of string

module CountryFisCode =
    let tryCreate (v: string) =
        if System.String.IsNullOrWhiteSpace v then None
        elif v.Length = 3 then Some(CountryFisCode(v.ToUpper()))
        else None

    let value (CountryFisCode v) = v


type Country =
    { Alpha2: Alpha2Code
      Alpha3: Alpha3Code
      FisCode: CountryFisCode }

type ICountries =
    abstract member GetAll: ct: CancellationToken -> Task<IEnumerable<Country>>
    abstract member GetByFisCode: fisCode: CountryFisCode * ct: CancellationToken -> Task<Country option>
