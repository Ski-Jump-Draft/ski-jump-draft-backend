module App.Domain.Shared.Utils.DomainEventFactory

open System
open App.Domain.Shared
open App.Domain.Shared.EventHelpers
open App.Domain.Time

let create<'T>
    (schemaVer: uint16)
    (occuredAt: DateTimeOffset)
    (id: Guid)
    (correlationId: Guid)
    (causationId: Guid option)
    (payload: 'T)
    : DomainEvent<'T> =

    let header =
        { EventId = id
          SchemaVer = schemaVer
          OccurredAt = occuredAt
          CorrelationId = Some correlationId
          CausationId = causationId }
        : EventHeader

    { Header = header; Payload = payload }
