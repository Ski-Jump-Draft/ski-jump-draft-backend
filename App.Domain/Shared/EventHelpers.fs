namespace App.Domain.Shared.EventHelpers

open System

type EventTimestamp = System.DateTimeOffset

[<Struct; CLIMutable>]
type EventHeader =
    { EventId: Guid
      SchemaVer: uint16
      OccurredAt: EventTimestamp
      CorrelationId: Guid option
      CausationId: Guid option }

module EventHeader =
    let create id schemaVersion occuredAt correlation causation =
        { EventId = id
          SchemaVer = schemaVersion
          OccurredAt = occuredAt
          CorrelationId = correlation
          CausationId = causation }