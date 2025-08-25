namespace App.Domain._2.Competition

type RoundIndex = RoundIndex of uint
module RoundIndexModule =
    let value (RoundIndex v) = v