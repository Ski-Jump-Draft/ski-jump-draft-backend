module App.Domain.Draft.Order

type Order =
    | Classic
    | Snake
    | RandomSeed of int
