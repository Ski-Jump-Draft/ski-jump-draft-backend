module App.Domain.Shared.Random

type IRandom =
    abstract member ShuffleList<'a> : seed: int -> list: 'a list -> 'a list
    /// From Min (inclusive) to Max (inclusive)
    abstract member RandomInt: Min: int * Max: int -> int
    abstract member NextUInt64: unit -> uint64
