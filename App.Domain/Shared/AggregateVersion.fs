module App.Domain.Shared.AggregateVersion

type AggregateVersion = AggregateVersion of uint
let zero = AggregateVersion.AggregateVersion 0u
let increment (AggregateVersion v) = AggregateVersion (v + 1u)
