namespace App.Domain.PreDraft

namespace App.Domain.PreDraft

open App
open App.Domain.Shared
open App.Domain.Shared.AggregateVersion
open App.Domain.PreDraft
open App.Domain.PreDraft.Event
open App.Domain.PreDraft.Phase

[<RequireQualifiedAccess>]
module Evolve =

    /// Rekonstruuje (lub aktualizuje) stan agregatu na podstawie pojedynczego zdarzenia.
    /// `stateOpt = None`  ⇒  zdarzenie inicjalizujące (PreDraftCreatedV1)
    let evolve (stateOpt: PreDraft option) (event: DomainEvent<PreDraftEventPayload>) : PreDraft =
        let ver = increment (stateOpt |> Option.map _.Version_ |> Option.defaultValue AggregateVersion.zero)

        match stateOpt, event.Payload with
        // ------------------------------------------------------------------
        // 1️⃣  ZDARZENIE TWORZĄCE STAN
        // ------------------------------------------------------------------
        | None, PreDraftCreatedV1 e ->
            { Id       = e.PreDraftId
              Version  = ver
              Phase    = InProgress (CompetitionIndex 0u, Domain.SimpleCompetition.CompetitionId System.Guid.Empty)  // ID uzupełni kolejne zdarzenie
              Settings = e.Settings }

        // ------------------------------------------------------------------
        // 2️⃣  AKTUALIZACJA ISTNIEJĄCEGO STANU
        // ------------------------------------------------------------------
        | Some st, PreDraftCompetitionStartedV1 e ->
            { st with
                Version = ver
                Phase   = InProgress (e.Index, e.CompetitionId) }

        | Some st, PreDraftCompetitionEndedV1 _ ->
            // dla command-side PreDraft nic nie zmieniamy – inkrementujemy tylko wersję
            { st with Version = ver }

        | Some st, PreDraftEndedV1 _ ->
            { st with
                Version = ver
                Phase   = Ended }

        // ------------------------------------------------------------------
        // 3️⃣  NIESPODZIEWANE KOMBINACJE (np. Created pojawia się po raz drugi)
        // ------------------------------------------------------------------
        | Some st, _ -> { st with Version = ver }
        | None   , _ -> invalidOp "Pierwsze zdarzenie w strumieniu musi być PreDraftCreatedV1."

    /// Składa stan z pełnej listy zdarzeń z kolejności chronologicznej.
    let evolveFromEvents (events: DomainEvent<PreDraftEventPayload> list) : PreDraft =
        events |> List.fold (fun acc ev -> evolve (Some acc) ev) (evolve None (List.head events))


