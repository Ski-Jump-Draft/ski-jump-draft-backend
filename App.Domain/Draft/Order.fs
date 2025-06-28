namespace App.Domain.Draft.Order

open System
open App.Domain.Draft

type OrderOption =
    | Classic
    | Snake
    | RandomSeed of uint64

/// Strategia kolejności draftu – interfejs obiektowy.
type IOrderStrategy =
    /// Ustal początkową kolejność uczestników na bazie ustawień i ziora.
    abstract member ComputeInitialOrder: participants: Participant.Id list * seed: uint64 -> Participant.Id list

    /// Wylicz nową kolejność i index następnego gracza po zakończonej rundzie.
    abstract member ComputeNextOrder:
        currentOrder: Participant.Id list * currentIndex: int * completedRounds: int * seed: uint64 ->
            Participant.Id list * int

/// Klasa dla klasycznego „round-robin”: kolejność się nie zmienia.
type ClassicOrderStrategy() =
    interface IOrderStrategy with
        member _.ComputeInitialOrder(participants, _seed) = participants

        member _.ComputeNextOrder(currentOrder, currentIndex, _rounds, _seed) =
            let nextIndex = (currentIndex + 1) % List.length currentOrder
            currentOrder, nextIndex

/// Klasa dla „snake”: co rundę zmienia się kierunek.
type SnakeOrderStrategy() =
    interface IOrderStrategy with
        member _.ComputeInitialOrder(participants, _seed) = participants

        member _.ComputeNextOrder(currentOrder, currentIndex, completedRounds, _seed) =
            let count = List.length currentOrder
            let forward = completedRounds % 2 = 0

            let nextIndex =
                if forward then (currentIndex + 1) % count
                elif currentIndex = 0 then 0
                else currentIndex - 1

            currentOrder, nextIndex

/// Klasa dla „random seed”: deterministyczne losowanie według ziarna + rund.
type RandomSeedOrderStrategy(baseSeed: uint64) =
    interface IOrderStrategy with
        member _.ComputeInitialOrder(participants, seed) =
            let rng = Random(int (baseSeed + seed))
            participants |> List.sortBy (fun _ -> rng.Next())

        member _.ComputeNextOrder(currentOrder, currentIndex, completedRounds, seed) =
            let rng = Random(int (baseSeed + seed + uint64 completedRounds))
            let perm = currentOrder |> List.sortBy (fun _ -> rng.Next())
            let count = List.length perm
            let current = currentOrder.[currentIndex]
            let posInPerm = perm |> List.findIndex ((=) current)
            perm, (posInPerm + 1) % count

module OrderStrategyFactory =
    let create (order: OrderOption) : IOrderStrategy =
        match order with
        | OrderOption.Classic -> ClassicOrderStrategy() :> IOrderStrategy
        | OrderOption.Snake -> SnakeOrderStrategy() :> IOrderStrategy
        | OrderOption.RandomSeed seed -> RandomSeedOrderStrategy(seed) :> IOrderStrategy
