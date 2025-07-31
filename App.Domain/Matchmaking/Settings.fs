namespace App.Domain.Matchmaking

open System

type Duration = Duration of TimeSpan
module Duration =
    let value (Duration v) = v

type Settings = {
    MinParticipants: PlayersCount
    MaxParticipants: PlayersCount
    MaxDuration: Duration
}

