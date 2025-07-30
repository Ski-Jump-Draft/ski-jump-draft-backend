module App.Domain.Game.Evolve

open App.Domain.Game
open App.Domain.Game.Event
open App.Domain.Game.Game
open App.Domain.Game.Participant
open App.Domain.Shared
open App.Domain.Shared.AggregateVersion

/// Applies a single event to the current aggregate state.
let evolve (state: Game) (event: DomainEvent<GameEventPayload>) =
    let version = AggregateVersion event.Header.AggregateVersion

    match event.Payload with
    | GameEventPayload.GameCreatedV1 e ->
        { Id = e.GameId
          Version = version
          //ServerId = e.ServerId
          Settings = e.Settings
          Participants = Participants.empty
          Phase = Phase.Break PhaseTag.PreDraftTag }

    // | GameEventPayload.ParticipantJoinedV1 e ->
    //     let participants =
    //         match Participants.add e.ParticipantId state.Participants with
    //         | Ok ps -> ps
    //         | Error _ -> state.Participants
    //
    //     { state with
    //         Participants = participants
    //         Version = version }

    | GameEventPayload.ParticipantLeftV1 e ->
        let participants = Participants.remove e.Participant state.Participants

        { state with
            Participants = participants
            Version = version }

    | GameEventPayload.PreDraftPhaseStartedV1 e ->
        { state with
            Phase = Phase.PreDraft e.PreDraftId
            Version = version }

    | GameEventPayload.PreDraftPhaseEndedV1 _ ->
        { state with
            Phase = Phase.Break PhaseTag.PreDraftTag
            Version = version }

    | GameEventPayload.DraftPhaseStartedV1 e ->
        { state with
            Phase = Phase.Draft e.DraftId
            Version = version }

    | GameEventPayload.DraftPhaseEndedV1 _ ->
        { state with
            Phase = Phase.Break PhaseTag.CompetitionTag
            Version = version }

    | GameEventPayload.CompetitionPhaseStartedV1 e ->
        { state with
            Phase = Phase.Competition e.CompetitionId
            Version = version }

    | GameEventPayload.CompetitionPhaseEndedV1 _ ->
        { state with
            Phase = Phase.Break PhaseTag.EndedTag
            Version = version }

    | GameEventPayload.GameEndedV1 e ->
        { state with
            Phase = Phase.Ended e.Ranking
            Version = version }

/// Evolves a full aggregate state from a historical list of domain events.
let evolveFromEvents (events: DomainEvent<GameEventPayload> list) : Game =
    events |> List.fold evolve Unchecked.defaultof<Game>
