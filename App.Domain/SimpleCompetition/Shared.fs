namespace App.Domain.SimpleCompetition

type GroupIndex = GroupIndex of uint
module GroupIndexModule =
    let value (GroupIndex v) = v

type RoundIndex = RoundIndex of uint
module RoundIndexModule =
    let value (RoundIndex v) = v