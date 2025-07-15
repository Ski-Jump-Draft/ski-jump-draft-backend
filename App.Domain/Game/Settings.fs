module App.Domain.Game.Settings

open System

type Error =
    | PlayersLimitTooFew of uint
    | PlayersLimitTooMany of uint

type ParticipantLimit = private ParticipantLimit of uint

module ParticipantLimit =
    let tryCreate (v: uint) =
        if v < 2u then Error(PlayersLimitTooFew(2u))
        elif v > 15u then Error(PlayersLimitTooMany(15u))
        else Ok(ParticipantLimit v)

    let value (ParticipantLimit v) = v

    let fits (count: uint) (limit: ParticipantLimit) = uint count <= value limit

module PhaseTransitionPolicy =
    type Error =
        | HostDecisionTimeoutTooLong of TimeSpan
        | AutostartAfterTimeTimeTooLong of TimeSpan
        | AutostartAfterTimeMinimalPlayersCountTooMany of uint

    type HostDecisionTimeout = private HostDecisionTimeout of TimeSpan

    module HostDecisionTimeout =
        let tryCreate (v: TimeSpan) =
            if v <= TimeSpan.FromSeconds(120L) then
                Ok(HostDecisionTimeout v)
            else
                Error(HostDecisionTimeoutTooLong v)

        let value (v: HostDecisionTimeout) = v

    module AutostartAfterTime =
        type Time = private Time of TimeSpan

        module Time =
            let tryCreate (v: TimeSpan) =
                if v <= TimeSpan.FromMinutes(5L) then
                    Ok v
                else
                    Error(AutostartAfterTimeTimeTooLong v)

            let value (v: Time) = v

        type MinimalPlayersCount = private MinimalPlayersCount of uint

        module MinimalPlayersCount =
            let tryCreate (v: uint) =
                if v < 1000u then
                    Ok v // TODO: GLOBAL_MAX_PLAYERS_IN_GAME / lub nie global, ale wciąż
                else
                    Error(AutostartAfterTimeMinimalPlayersCountTooMany v)

            let value (v: MinimalPlayersCount) = v

        type FailurePolicy =
            | Retry
            | DelegateControlToHost

    type StartingMatchmaking = | None // nie ma szczególnych metod na rozpoczęcie matchmakingu. TODO

    type EndingMatchmaking =
        | HostDecides of HostDecisionTimeout option
        | AutoAfter of
            AutostartAfterTime.Time *
            AutostartAfterTime.MinimalPlayersCount *
            AutostartAfterTime.FailurePolicy
        | AutoWhenFull

    type StartingDraft =
        | HostDecides of HostDecisionTimeout
        | AutoAfter of TimeSpan

    type StartingSimulating =
        | HostDecides of HostDecisionTimeout
        | AutoAfter of TimeSpan

    type EndingSimulation = | None // nie ma szczególnych metod na zakończenie gry. TODO

type Settings =
    { ParticipantLimit: ParticipantLimit
      PreDraftSettings: App.Domain.PreDraft.Settings.Settings
      DraftSettings: App.Domain.Draft.Settings.Settings
      CompetitionSettings: App.Domain.Game.Competition.Settings
      StartingMatchmakingPolicy: PhaseTransitionPolicy.StartingMatchmaking
      EndingMatchmakingPolicy: PhaseTransitionPolicy.EndingMatchmaking
      StartingDraftPolicy: PhaseTransitionPolicy.StartingDraft
      StartingCompetitionPolicy: PhaseTransitionPolicy.StartingSimulating
      EndingSimulationPolicy: PhaseTransitionPolicy.EndingSimulation
      HillId: Hill.Id }
