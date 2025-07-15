module App.Domain.Game.Evolve

open App.Domain.Game
open App.Domain.Game.Event
open App.Domain.Game.Game
open App.Domain.Shared
open App.Domain.Shared.AggregateVersion

/// Applies a single event to the current aggregate state.
let evolve (state: Game) =
    function
    | GameEventPayload.GameCreatedV1 e ->
        { Id = e.GameId
          Version = AggregateVersion 0u
          HostId = e.HostId
          Settings = e.Settings
          Participants = Participants.empty
          Phase = Phase.SettingUp }

    | GameEventPayload.ParticipantJoinedV1 e ->
        // The event is trusted – any domain‑level checks were made beforehand.
        let participants =
            match Participants.add e.ParticipantId state.Participants with
            | Ok ps -> ps
            | Error _ -> state.Participants // should never happen for a persisted event
        { state with Participants = participants }

    | GameEventPayload.ParticipantLeftV1 e ->
        let participants = Participants.remove e.ParticipantId state.Participants
        { state with Participants = participants }

    | GameEventPayload.MatchmakingPhaseStartedV1 _ ->
        { state with Phase = Phase.Matchmaking }

    | GameEventPayload.MatchmakingPhaseEndedV1 _ ->
        { state with Phase = Phase.Break PhaseTag.PreDraftTag }

    | GameEventPayload.PreDraftPhaseStartedV1 e ->
        { state with Phase = Phase.PreDraft e.PreDraftId }

    | GameEventPayload.PreDraftPhaseEndedV1 _ ->
        { state with Phase = Phase.Break PhaseTag.PreDraftTag }

    | GameEventPayload.DraftPhaseStartedV1 e ->
        { state with Phase = Phase.Draft e.DraftId }

    | GameEventPayload.DraftPhaseEndedV1 _ ->
        { state with Phase = Phase.Break PhaseTag.CompetitionTag }

    | GameEventPayload.CompetitionPhaseStartedV1 e ->
        { state with Phase = Phase.Competition e.CompetitionId }

    | GameEventPayload.CompetitionPhaseEndedV1 _ ->
        { state with Phase = Phase.Break PhaseTag.EndedTag }

    | GameEventPayload.GameEndedV1 e ->
        { state with Phase = Phase.Ended e.Results }

/// Evolves a full aggregate state from a historical list of domain events.
let evolveFromEvents (events: DomainEvent<GameEventPayload> list) : Game =
    events
    |> List.map (fun e -> e.Payload)
    |> List.fold evolve Unchecked.defaultof<Game> // GameCreated must be the first event!
