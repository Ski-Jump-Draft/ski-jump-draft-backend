module App.Domain.Shared.Random

type IRandom =
    abstract member ShuffleList<'a> : int -> 'a list -> 'a list
    /// From Min (inclusive) to Max (inclusive)
    abstract member RandomInt: Min: int * Max: int -> int
