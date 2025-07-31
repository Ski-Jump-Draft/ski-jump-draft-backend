namespace App.Domain.GameWorld

open App.Domain.GameWorld.Event

open HillTypes

module Hill =
    type Error =
        | InGameRecordIsSimpleReference
        | HsPointNotGreaterThanKPoint of KPoint: double * HsPoint: double
// | InvalidGeometryUpdate of KPointOpt: KPoint option * HsPointOpt: HsPoint option * Message: string

open Hill

type Hill =
    private
        { Id: HillTypes.Id
          Location: Location
          Name: Name
          CountryId: Country.Id
          KPoint: KPoint
          HsPoint: HsPoint
          RealRecord: Record
          InGameRecord: Record }

    member this.Id_ = this.Id
    member this.KPoint_ = this.KPoint
    member this.HsPoint_ = this.HsPoint

    static member Create
        id
        //status
        location
        name
        countryId
        kPoint
        hsPoint
        realRecord
        inGameRecord // TODO: Na pewno tutaj?
        : Result<Hill * Event.HillEventPayload list, Error> =
        if inGameRecord.SetterReference.IsSimple then
            Error(Error.InGameRecordIsSimpleReference)
        elif KPoint.value kPoint > HsPoint.value hsPoint then
            Error(Error.HsPointNotGreaterThanKPoint(KPoint.value kPoint, HsPoint.value hsPoint))
        else
            let state =
                { Id = id
                  Location = location
                  Name = name
                  CountryId = countryId
                  KPoint = kPoint
                  HsPoint = hsPoint
                  RealRecord = realRecord
                  InGameRecord = inGameRecord }

            let event =
                { HillId = id
                  //Status = status
                  Location = location
                  Name = name
                  CountryId = countryId
                  KPoint = kPoint
                  HsPoint = hsPoint
                  RealRecord = realRecord
                  InGameRecord = inGameRecord }
                : Event.HillCreatedV1

            Ok(state, [ HillEventPayload.HillCreatedV1 event ])

    // member this.UpdateInfo
    //     (nameOpt: Name option)
    //     (realRecordOpt: RealRecord option)
    //     : Result<Hill * HillEventPayload list, Error> =
    //
    //     if nameOpt.IsNone && realRecordOpt.IsNone then
    //         Ok(this, [])
    //     else
    //         let newName = defaultArg nameOpt this.Name
    //         let newRecord = defaultArg realRecordOpt this.RealRecord
    //
    //         let updated =
    //             { this with
    //                 Name = newName
    //                 RealRecord = newRecord }
    //
    //         let event =
    //             { HillId = this.Id
    //               Name = nameOpt
    //               RealRecord = realRecordOpt }
    //             : Event.HillInfoUpdatedV1
    //
    //         Ok(updated, [ HillEventPayload.HillInfoUpdatedV1 event ])
    //
    //
    //
    // member this.UpdateGeometry
    //     (kPointOpt: KPoint option)
    //     (hsPointOpt: HsPoint option)
    //     : Result<Hill * Event.HillEventPayload list, Error> =
    //
    //     match kPointOpt, hsPointOpt with
    //     | None, None -> Error(InvalidGeometryUpdate(kPointOpt, hsPointOpt, "Either kPoint or hsPoint must be Some"))
    //
    //     | Some kPoint, Some hsPoint ->
    //         if HsPoint.value hsPoint > KPoint.value kPoint then
    //             let event =
    //                 { Event.HillGeometryUpdatedV1.HillId = this.Id
    //                   KPoint = Some kPoint
    //                   HsPoint = Some hsPoint }
    //
    //             Ok(
    //                 { this with
    //                     KPoint = kPoint
    //                     HsPoint = hsPoint },
    //                 [ Event.HillEventPayload.HillGeometryUpdatedV1 event ]
    //             )
    //         else
    //             Ok(this, [])
    //
    //     | Some kPoint, None ->
    //         let event =
    //             { Event.HillGeometryUpdatedV1.HillId = this.Id
    //               KPoint = Some kPoint
    //               HsPoint = None }
    //
    //         Ok({ this with KPoint = kPoint }, [ Event.HillEventPayload.HillGeometryUpdatedV1 event ])
    //
    //     | None, Some hsPoint ->
    //         if HsPoint.value hsPoint > KPoint.value this.KPoint then
    //             let event =
    //                 { Event.HillGeometryUpdatedV1.HillId = this.Id
    //                   KPoint = None
    //                   HsPoint = Some hsPoint }
    //
    //             Ok({ this with HsPoint = hsPoint }, [ Event.HillEventPayload.HillGeometryUpdatedV1 event ])
    //         else
    //             Ok(this, [])

    member this.TryUpdateInGameRecord(distance: Record.Distance) =
        if (Record.Distance.value distance) > (Record.Distance.value this.InGameRecord.Distance) then
            let inGameRecord =
                { this.InGameRecord with
                    Distance = distance }

            Some
                { this with
                    InGameRecord = inGameRecord }
        else
            None
