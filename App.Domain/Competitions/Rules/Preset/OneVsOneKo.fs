namespace App.Domain.Competitions.Rules.Preset

open App.Domain.CustomStrategies

module OneVsOneKo =
    type LosingPolicy =
        | SingleElimination
        | DoubleElimination

    [<Struct>]
    type FixedParticipantsCount = private FixedParticipantsCount of int with
        static member TryCreate(v:int) : FixedParticipantsCount option =
            if v > 0 && v < 1000 then Some(FixedParticipantsCount v) else None
        static member Value(FixedParticipantsCount v) = v

    type ParticipantsPolicy =
        | Any
        | Fixed of FixedParticipantsCount

    type ExAequoPolicy =
        | Everyone
        | Draw
        | Custom of CustomStrategy.Ref

    type ThirdPlaceMatchPolicy =
        | Play
        | DontPlay

    [<Struct>]
    type WinsNumber = private WinsNumber of int with
        static member TryCreate(v:int) : WinsNumber option =
            if v > 0 && v < 20 then Some(WinsNumber v) else None
        static member Value(WinsNumber v) = v

    [<Struct>]
    type FixedRounds = private FixedRounds of int with
        static member TryCreate(v:int) : FixedRounds option =
            if v > 0 && v < 20 then Some(FixedRounds v) else None
        static member Value(FixedRounds v) = v

    type FixedRoundsTiePolicy =
        | OneRoundOvertime
        | DrawWinner
        | EveryoneAdvance
        | NoOneAdvance

    type WinCondition =
        | FirstTo of WinsNumber

open OneVsOneKo
type OneVsOneKo = private {
    ParticipantsPolicy      : ParticipantsPolicy
    LosingPolicy           : LosingPolicy
    DecisiveExAequoPolicy  : ExAequoPolicy
    ThirdPlaceMatchPolicy  : ThirdPlaceMatchPolicy
    WinCondition           : WinCondition
} with
    static member Create(participantsPolicy, losingPolicy, decisiveExAequoPolicy, thirdPlaceMatchPolicy, winCondition) =
        { ParticipantsPolicy      = participantsPolicy
          LosingPolicy           = losingPolicy
          DecisiveExAequoPolicy  = decisiveExAequoPolicy
          ThirdPlaceMatchPolicy  = thirdPlaceMatchPolicy
          WinCondition           = winCondition }
