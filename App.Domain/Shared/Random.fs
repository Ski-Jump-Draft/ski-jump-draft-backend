module App.Domain.Shared.Random

type IRandom =
    abstract member ShuffleList<'a> : seed: int -> list: 'a list -> 'a list
    /// From Min (inclusive) to Max (inclusive)
    abstract member NextInt: Min: int * Max: int -> int
    abstract member NextDouble: unit -> uint64
    abstract member NextUInt64: unit -> uint64