namespace App.Domain.GameWorld

open App.Domain.GameWorld.Event

module Hill =

    [<Struct>]
    type KPoint = private KPoint of double

    module KPoint =
        let tryCreate (v: double) =
            if v >= 0.0 then Some(KPoint v) else None

        let value (KPoint v) = v

    [<Struct>]
    type HSPoint = private HSPoint of double

    module HSPoint =
        let tryCreate (v: double) =
            if v >= 0.0 then Some(HSPoint v) else None

        let value (HSPoint v) = v

    [<Struct>]
    type Name = Name of string

    [<Struct>]
    type Location = Location of string

open Hill

type Hill =
    private
        { Id: HillId
          Location: Location
          Name: Name
          CountryId: Country.Id
          KPoint: KPoint
          HSPoint: HSPoint }

    member this.Id_ = this.Id
    member this.KPoint_ = this.KPoint
    member this.HSPoint_ = this.HSPoint

    static member Create
        id
        location
        name
        countryId
        kPoint
        hsPoint
        : Result<Hill * Event.GameWorldEventPayload list, System.Exception> =
        let state =
            { Id = id
              Location = location
              Name = name
              CountryId = countryId
              KPoint = kPoint
              HSPoint = hsPoint }

        let event =
            { HillId = id
              Location = string (location)
              Name = string (name)
              CountryId = string (countryId)
              KPoint = KPoint.value kPoint
              HSPoint = HSPoint.value hsPoint }
            : Event.HillCreatedV1

        Ok(state, [ GameWorldEventPayload.HillCreatedV1 event ])

    member this.Delete =
        let event = { HillId = this.Id }: Event.HillRemovedV1
        Ok(this, [ GameWorldEventPayload.HillRemovedV1 event ])
