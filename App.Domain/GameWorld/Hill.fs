namespace App.Domain.GameWorld

open App.Domain.GameWorld.Event

open App.Domain.GameWorld.HillTypes.Record
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
          InGameRecord: Record option }

    member this.Id_ = this.Id
    member this.KPoint_ = this.KPoint
    member this.HsPoint_ = this.HsPoint

    static member Create
        id
        location
        name
        countryId
        kPoint
        hsPoint
        realRecord
        : Result<Hill * Event.HillEventPayload list, Error> =
        if KPoint.value kPoint > HsPoint.value hsPoint then
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
                  InGameRecord = None }

            let event =
                { HillId = id
                  Location = location
                  Name = name
                  CountryId = countryId
                  KPoint = kPoint
                  HsPoint = hsPoint
                  RealRecord = realRecord
                  InGameRecord = None }
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

    member this.TryUpdateInGameRecord
        (setterReference: Record.SetterReference, distance: Record.Distance)
        : Result<Hill * Event.HillEventPayload list, Error> =
        match setterReference with
        | Simple _ -> Error Error.InGameRecordIsSimpleReference
        | GameWorldJumper gameWorldJumperId ->
            let shouldUpdate =
                this.InGameRecord.IsNone
                || Distance.value distance > Distance.value this.InGameRecord.Value.Distance

            if not shouldUpdate then
                Ok(this, [])
            else
                let inGameRecord =
                    { SetterReference = setterReference
                      Distance = distance }

                let recordUpdatedEvent =
                    HillEventPayload.HillInGameRecordUpdatedV1
                        { HillId = this.Id
                          GameWorldJumperId = gameWorldJumperId
                          Distance = Distance.value distance }

                Ok(
                    { this with
                        InGameRecord = Some inGameRecord },
                    [ recordUpdatedEvent ]
                )
