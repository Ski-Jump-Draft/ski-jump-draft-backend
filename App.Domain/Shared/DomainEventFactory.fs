module App.Domain.Shared.Utils.DomainEventFactory

open System
open App.Domain.Shared
open App.Domain.Shared.EventHelpers
open App.Domain.Time

let create<'T>
    (aggregateVer: uint)
    (schemaVer: uint16)
    (occuredAt: DateTimeOffset)
    (id: Guid)
    (correlationId: Guid)
    (causationId: Guid)
    (payload: 'T)
    : DomainEvent<'T> =

    let header =
        { EventId = id
          AggregateVersion = aggregateVer
          SchemaVer = schemaVer
          OccurredAt = occuredAt
          CorrelationId = correlationId
          CausationId = causationId }
        : EventHeader

    { Header = header; Payload = payload }
