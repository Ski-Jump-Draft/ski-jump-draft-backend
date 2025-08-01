namespace App.Domain.Shared.EventHelpers

open System

type EventTimestamp = System.DateTimeOffset

[<CLIMutable>]
type EventHeader =
    {
        EventId: Guid
        /// From 0
        AggregateVersion: uint
        /// From 1
        SchemaVer: uint16
        OccurredAt: EventTimestamp
        CorrelationId: Guid
        CausationId: Guid
    }

module EventHeader =
    let create id aggregateVersion schemaVersion occuredAt correlation causation =
        { EventId = id
          AggregateVersion = aggregateVersion
          SchemaVer = schemaVersion
          OccurredAt = occuredAt
          CorrelationId = correlation
          CausationId = causation }
