module App.Domain.Shared.Random

type IRandom =
    abstract member ShuffleList<'a>: int -> 'a list -> 'a list