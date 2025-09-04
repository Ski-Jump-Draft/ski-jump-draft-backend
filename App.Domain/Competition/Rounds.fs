namespace App.Domain.Competition

type RoundIndex = RoundIndex of uint
module RoundIndexModule =
    let value (RoundIndex v) = v