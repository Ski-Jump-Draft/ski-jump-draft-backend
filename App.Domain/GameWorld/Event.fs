module App.Domain.GameWorld.Event

type HillStatusDto = | Retired

type HillCreatedV1 =
    { HillId: HillTypes.Id
      //Status: HillTypes.Status
      Location: HillTypes.Location
      Name: HillTypes.Name
      CountryId: Country.Id
      KPoint: HillTypes.KPoint
      HsPoint: HillTypes.HsPoint
      RealRecord: HillTypes.Record
      InGameRecord: HillTypes.Record option }

type HillRetiredV1 = { HillId: HillTypes.Id }

type HillGeometryUpdatedV1 =
    { HillId: HillTypes.Id
      KPoint: HillTypes.KPoint option
      HsPoint: HillTypes.HsPoint option }
    
type HillInGameRecordUpdatedV1 = {
    HillId: HillTypes.Id
    GameWorldJumperId: JumperTypes.Id
    Distance: double
}

type HillEventPayload =
    | HillCreatedV1 of HillCreatedV1
    | HillRemovedV1 of HillRetiredV1
    | HillGeometryUpdatedV1 of HillGeometryUpdatedV1
    | HillInGameRecordUpdatedV1 of HillInGameRecordUpdatedV1

module Versioning =
    let schemaVersion =
        function
        | HillCreatedV1 _ -> 1us
        | HillRemovedV1 _ -> 1us
        | HillGeometryUpdatedV1 _ -> 1us
        | HillInGameRecordUpdatedV1 _ -> 1us
