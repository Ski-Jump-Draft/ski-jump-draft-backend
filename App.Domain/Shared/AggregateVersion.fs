module App.Domain.Shared.AggregateVersion

type AggregateVersion = AggregateVersion of uint
let increment (AggregateVersion v) = AggregateVersion (v + 1u)
