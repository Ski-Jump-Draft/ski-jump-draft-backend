namespace App.Domain.Shared

type IGuid =
    abstract member NewGuid: unit -> System.Guid

module Ids =
    [<Struct>]
    type HostId = HostId of System.Guid

    [<Struct>]
    type RegionId = RegionId of System.Guid

    [<Struct>]
    type ServerId = ServerId of System.Guid

    [<Struct>]
    type GameId = GameId of System.Guid

    [<Struct>]
    type PlayerId = PlayerId of System.Guid

    [<Struct>]
    type JumperId = JumperId of System.Guid

    [<Struct>]
    type HillId = HillId of System.Guid

    [<Struct>]
    type CountryId = CountryId of System.Guid

    [<Struct>]
    type DraftId = DraftId of System.Guid

    [<Struct>]
    type CompetitionId = CompetitionId of System.Guid

    [<Struct>]
    type CompetitionRulesPresetId = CompetitionRulesPresetId of System.Guid

open Ids

module Id =
    let newHostId (gen: IGuid) = HostId(gen.NewGuid())
    let newRegionId (gen: IGuid) = RegionId(gen.NewGuid())
    let newServerId (gen: IGuid) = ServerId(gen.NewGuid())
    let newGameId (gen: IGuid) = GameId(gen.NewGuid())
    let newPlayerId (gen: IGuid) = PlayerId(gen.NewGuid())
    let newJumperId (gen: IGuid) = JumperId(gen.NewGuid())
    let newHillId (gen: IGuid) = HillId(gen.NewGuid())
    let newCountryId (gen: IGuid) = CountryId(gen.NewGuid())
    let newDraftId (gen: IGuid) = DraftId(gen.NewGuid())
    let newCompetitionId (gen: IGuid) = CompetitionId(gen.NewGuid())
    let newCompetitionRulesPresetId (gen: IGuid) = CompetitionRulesPresetId(gen.NewGuid())

    let valueOfHostId (HostId s) = s
    let valueOfRegionId (RegionId s) = s
    let valueOfServerId (ServerId s) = s
    let valueOfGameId (GameId s) = s
    let valueOfPlayerId (PlayerId s) = s
    let valueOfJumperId (JumperId s) = s
    let valueOfHillId (HillId s) = s
    let valueOfCountryId (CountryId s) = s
    let valueOfDraftId (DraftId s) = s
    let valueOfCompetitionId (CompetitionId s) = s
    let valueOfCompetitionRulesPresetId (CompetitionRulesPresetId s) = s
