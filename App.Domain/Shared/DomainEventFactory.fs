module App.Domain.Shared.Utils.DomainEventFactory

open System
open App.Domain.Shared
open App.Domain.Shared.EventHelpers
open App.Domain.Time

let create<'T>
    (schemaVer: uint16)
    (clock: IClock)
    (guid: IGuid)
    (correlationId: Guid)
    (causationId: Guid option)
    (payload: 'T)
    : DomainEvent<'T> =

    let header =
        let id = guid.NewGuid()

        { EventId = id
          SchemaVer = schemaVer
          OccurredAt = clock.UtcNow
          CorrelationId = Some correlationId
          CausationId = causationId }
        : EventHeader

    { Header = header; Payload = payload }
