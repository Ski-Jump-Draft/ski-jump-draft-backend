module App.Domain.GameWorld.Event


[<Struct; CLIMutable>]
type HillCreatedV1 =
    { HillId: HillId
      Location: string
      Name: string
      CountryId: string
      KPoint: double
      HSPoint: double }

[<Struct; CLIMutable>]
type HillRemovedV1 = { HillId: HillId }

type GameWorldEventPayload =
    | HillCreatedV1 of HillCreatedV1
    | HillRemovedV1 of HillRemovedV1

module Versioning =
    let schemaVersion =
        function
        | HillCreatedV1 _ -> 1us
        | HillRemovedV1 _ -> 1us
