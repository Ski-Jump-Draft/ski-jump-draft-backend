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

type DomainEvent<'T> = { Header: EventHeader; Payload: 'T }

module DomainEvent =
    let create eventId schemaVersion occuredAt correlatioNid causationId payload =
        { Header =
            { EventId = eventId
              SchemaVer = schemaVersion
              OccurredAt = occuredAt
              CorrelationId = correlatioNid
              CausationId = causationId }
          Payload = payload }
