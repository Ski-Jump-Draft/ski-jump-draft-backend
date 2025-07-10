namespace App.Domain.Shared

open System
open App.Domain.Shared.EventHelpers

type DomainEvent<'T> = { Header: EventHeader; Payload: 'T }

// module DomainEvent =
//     let create eventId schemaVersion occuredAt correlatioNid causationId payload =
//         { Header =
//             { EventId = eventId
//               SchemaVer = schemaVersion
//               OccurredAt = occuredAt
//               CorrelationId = correlatioNid
//               CausationId = causationId }
//           Payload = payload }