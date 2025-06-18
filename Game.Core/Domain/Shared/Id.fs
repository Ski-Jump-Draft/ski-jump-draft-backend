// module Game.Core.Domain.Id

namespace Game.Core.Domain.Shared
module Ids =
    [<Struct>]
    type ServerId = | ServerId of string    
    [<Struct>]
    type GameId   = | GameId   of string
    [<Struct>]
    type PlayerId = | PlayerId of string
    [<Struct>]
    type JumperId = | JumperId of string
    [<Struct>]
    type HillId   = | HillId   of string
    [<Struct>]
    type DraftId  = | DraftId of string
    [<Struct>]
    type CompetitionId  = | CompetitionId of string
    [<Struct>]
    type CompetitionRulesPresetId = | CompetitionRulesPresetId of string

open Ids

open Ids

module Id =
    let newServerId () = ServerId(System.Guid.NewGuid().ToString())
    let newGameId   () = GameId(System.Guid.NewGuid().ToString())
    let newPlayerId () = PlayerId(System.Guid.NewGuid().ToString())
    let newJumperId () = JumperId(System.Guid.NewGuid().ToString())
    let newHillId   () = HillId(System.Guid.NewGuid().ToString())
    let newDraftId  () = DraftId(System.Guid.NewGuid().ToString())
    let newCompetitionId  () = CompetitionId(System.Guid.NewGuid().ToString())
    let newCompetitionRulesPresetId  () = CompetitionRulesPresetId(System.Guid.NewGuid().ToString())

    let valueOfServerId (ServerId s) = s
    let valueOfGameId   (GameId s)   = s
    let valueOfPlayerId (PlayerId s) = s
    let valueOfJumperId (JumperId s) = s
    let valueOfHillId   (HillId s)   = s
    let valueOfDraftId  (DraftId s)  = s
    let valueOfCompetitionId (CompetitionId s) = s
    let valueOfCompetitionRulesPresetId (CompetitionRulesPresetId s)  = s