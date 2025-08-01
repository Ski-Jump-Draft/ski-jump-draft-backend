module App.Domain.Competition.Evolve

open App.Domain.Competition.Phase
open App.Domain.Shared
open App.Domain.Competition
open App.Domain.Competition.Event

type EngineInitializer = CompetitionCreatedV1 -> Engine.IEngine

let apply (init: EngineInitializer) (state: Competition option) (ev: CompetitionEventPayload) : Competition option =
    match state, ev with
    | None, CompetitionCreatedV1 payload ->
        let engine = init payload

        Some
            { Id = payload.CompetitionId
              Version = AggregateVersion.zero
              Phase = Phase.NotStarted
              Engine = engine }

    | Some c, CompetitionJumpRegisteredV1 j ->
        let _snapshot, _events = c.Engine.RegisterJump j.Jump

        Some
            { c with
                Version = AggregateVersion.increment c.Version_ }

    | Some c, CompetitionRoundStartedV1 e ->
        Some
            { c with
                Phase = Phase.Running(RoundIndex e.RoundIndex) }

    | Some c, CompetitionRoundEndedV1 e ->
        Some
            { c with
                Phase = Phase.Break(RoundIndex(e.RoundIndex + 1u)) }

    // -------- snapshot (gdy będziesz emitował) --------
    // | Some c, SnapshotSavedV1 s ->
    //     c.Engine.LoadSnapshot s.Snapshot
    //     Some c

    | s, _ -> s

let fold (init: EngineInitializer) (events: seq<CompetitionEventPayload>) : Competition =
    events
    |> Seq.fold (apply init) None
    |> Option.defaultWith (fun () -> invalidOp "No events to fold Competition")
