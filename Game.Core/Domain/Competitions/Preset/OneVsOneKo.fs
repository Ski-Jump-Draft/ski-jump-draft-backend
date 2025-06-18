namespace Game.Core.Domain.Competitions.Preset

open Game.Core.Domain

module OneVsOneKo =
  type LosingPolicy =
    | SingleElimination
    | DoubleElimination
    // | Custom TODO: Dll?

  [<Struct>]
  type FixedParticipantsCount = private FixedParticipantsCount of int
  module FixedParticipantsCount =
    let tryCreate v = if v > 0 && v < 1000 then Some (FixedParticipantsCount v) else None
    let value (FixedParticipantsCount v) = v
    
  type ParticipantsPolicy =
    | Any
    | Fixed of FixedParticipantsCount
    
  type ExAequoPolicy =
    | Everyone // Oboje przechodzą
    | Draw // Losowanie, kto przechodzi
    | Custom of Strategies.StrategyRef // Właściwa implementacja w warstwie Infra
    
  type ThirdPlaceMatchPolicy =
    | Play
    | DontPlay
    
  [<Struct>]
  type WinsNumber = private WinsNumber of int
  module WinsNumber =
    let tryCreate v = if v > 0 && v < 20 then Some (WinsNumber v) else None
    let value (WinsNumber v) = v
    
  [<Struct>]
  type FixedRounds = private FixedRounds of int
  module FixedRounds =
    type TiePolicy =
      | OneRoundOvertime
      | DrawWinner
      | EveryoneAdvance
      | NoOneAdvance

    let tryCreate v = if v > 0 && v < 20 then Some (FixedRounds v) else None
    let value (FixedRounds v) = v
    
  type WinCondition =
    | FirstTo of WinsNumber
    // | FixedRounds of FixedRounds * FixedRounds.TiePolicy
  
  type Definition = private {
    ParticipantsPolicy: ParticipantsPolicy
    LosingPolicy: LosingPolicy
    DecisiveExAequoPolicy: ExAequoPolicy
    ThirdPlaceMatchPolicy: ThirdPlaceMatchPolicy
    WinCondition: WinCondition
  }
  
  let tryCreate participantsPolicy losingPolicy decisiveExAequoPolicy thirdPlaceMatchPolicy winCondition  =
    {
      ParticipantsPolicy = participantsPolicy
      LosingPolicy = losingPolicy
      DecisiveExAequoPolicy = decisiveExAequoPolicy
      ThirdPlaceMatchPolicy = thirdPlaceMatchPolicy
      WinCondition = winCondition
    }
    