namespace App.Domain.SimpleCompetition

open App.Domain.Shared
open App.Domain.Shared.AggregateVersion
open App.Domain.SimpleCompetition
open App.Domain.SimpleCompetition.Competition
open App.Domain.SimpleCompetition.Event
open App.Domain.SimpleCompetition.Jump
open App.Domain.SimpleCompetition.JumpResult

module Evolve =

    // ---------- małe konwersje DTO -> VO ----------------------------------

    let private jumpFromDto (dto: JumpDtoV1) : Jump =
        { Id = dto.Id
          CompetitorId = dto.CompetitorId
          Distance = Distance.tryCreate dto.Distance |> Result.toOption |> Option.get
          WindAverage = WindAverage.FromDouble dto.WindAverage
          JudgeNotes = JudgeNotes.tryCreate dto.JudgeNotes |> Result.toOption |> Option.get }

    let private hillFromDto (dto: CompetitionHillDtoV1) : Hill =
        { Id = dto.Id
          KPoint = Hill.KPoint.tryCreate dto.KPoint |> Option.get
          HsPoint = Hill.HsPoint.tryCreate dto.HsPoint |> Option.get
          GatePoints = Hill.GatePoints.tryCreate dto.GatePoints |> Option.get
          HeadwindPoints = Hill.WindPoints.tryCreate dto.HeadwindPoints |> Option.get
          TailwindPoints = Hill.WindPoints.tryCreate dto.TailwindPoints |> Option.get }

    let roundLimitOfDto =
        function
        | RoundLimitDtoV1.NoneLimit -> RoundLimit.NoneLimit
        | RoundLimitDtoV1.Soft v -> RoundLimitValue.tryCreate v |> Result.toOption |> Option.get |> RoundLimit.Soft
        | RoundLimitDtoV1.Exact(v, criteria) ->
            let value = RoundLimitValue.tryCreate v |> Result.toOption |> Option.get

            let parsed =
                match criteria with
                | "LongestJump" -> TieBreakerCriteria.LongestJump
                | "BestJudgePoints" -> TieBreakerCriteria.BestJudgePoints
                | "HighestBib" -> TieBreakerCriteria.HighestBib
                | "LowestBib" -> TieBreakerCriteria.LowestBib
                | _ -> TieBreakerCriteria.Random

            RoundLimit.Exact(value, parsed)

    let roundSettingsOfDto (dto: RoundSettingsDtoV1) : RoundSettings =
        { RoundLimit = roundLimitOfDto dto.RoundLimit
          SortStartlist = dto.SortStartlist
          ResetPoints = dto.ResetPoints
          GroupSettings =
            dto.GroupIndexesToSort
            |> Option.map (fun indexes ->
                { GroupIndexesToSort = indexes |> List.map uint32 |> List.map GroupIndex |> Set.ofList }) }

    let settingsOfDto (dto: CompetitionSettingsDtoV1) : Settings =
        dto.RoundSettings
        |> List.map roundSettingsOfDto
        |> Settings.Create
        |> Result.toOption
        |> Option.get // w DTO była poprawna – jeśli nie, błąd w migracji

    let private startlistMarkDone cid (sl: Startlist) =
        match sl.MarkJumpDone cid with
        | Ok s -> s
        | Error _ -> sl // jeżeli z jakiegoś powodu nie udało się – ignorujemy

    // ---------- evolve pojedynczego eventu --------------------------------

    let evolve (state: Competition option) (event: DomainEvent<CompetitionEventPayload>) : Competition =

        // gdy nie ma jeszcze state – szukamy eventu Create*
        match state, event.Payload with
        | None, IndividualCompetitionCreatedV1 e ->
            let settings = (* rekonstrukcja Settings z DTO *) failwith "Settings mapper"
            let hill = hillFromDto e.Hill
            let comps = e.Competitors |> List.map (fun c -> Competitor.Create c.Id c.TeamId)

            Competition.CreateIndividual(e.CompetitionId, AggregateVersion.zero, settings, hill, comps, e.StartingGate)
            |> Result.toOption
            |> Option.get
            |> fst

        | None, TeamCompetitionCreatedV1 e ->
            let settings = failwith "Settings mapper"
            let hill = hillFromDto e.Hill

            let teams =
                e.Teams
                |> List.map (fun t ->
                    let cs = t.Competitors |> List.map (fun c -> Competitor.Create c.Id (Some t.Id))
                    Team.Create t.Id cs)

            Competition.CreateTeam(e.CompetitionId, AggregateVersion.zero, settings, hill, teams, e.StartingGate)
            |> Result.toOption
            |> Option.get
            |> fst

        // istnieje stan – patch-ujemy wg eventu
        | Some state, payload ->

            let nextVersion = increment state.Version

            match payload with
            | CompetitionStartedV1 _ ->
                { state with
                    Version = nextVersion
                    Status =
                        match state.Status with
                        | NotStarted gateState -> RoundInProgress(gateState, RoundIndex(0u), None)
                        | _ -> state.Status }

            | CompetitionRoundStartedV1 e ->
                { state with
                    Version = nextVersion
                    Status =
                        match state.Status with
                        | RoundInProgress(g, _, _) -> RoundInProgress(g, e.RoundIndex, None)
                        | Suspended(g, _, _) -> Suspended(g, e.RoundIndex, None)
                        | _ -> state.Status }

            | CompetitionGroupStartedV1 e ->
                { state with
                    Version = nextVersion
                    Status =
                        match state.Status with
                        | RoundInProgress(g, r, _) when r = e.RoundIndex -> RoundInProgress(g, r, Some e.GroupIndex)
                        | Suspended(g, r, _) when r = e.RoundIndex -> Suspended(g, r, Some e.GroupIndex)
                        | _ -> state.Status }

            | JumpAddedV1 e ->
                let jumpFromDto = jumpFromDto e.Jump

                let jumpResult =
                    { Id = e.JumpResultId
                      Jump = jumpFromDto
                      CompetitorId = jumpFromDto.CompetitorId
                      TeamId = e.TeamId
                      RoundIndex =
                        match state.Status with
                        | RoundInProgress(_, r, _) -> r
                        | _ -> RoundIndex 0u
                      GroupIndex =
                        match state.Status with
                        | RoundInProgress(_, _, g) -> g
                        | _ -> None
                      JudgePoints =
                        match e.JudgePoints with
                        | Some x -> JudgePoints.tryCreate x |> Result.toOption
                        | None -> None
                      GatePoints =
                        match e.GatePoints with
                        | Some x -> Some(GatePoints x)
                        | None -> None
                      WindPoints =
                        match e.WindPoints with
                        | Some x -> Some(WindPoints x)
                        | None -> None
                      TotalPoints = TotalPoints e.TotalPoints }
                    : JumpResult

                let newResults =
                    { state.Results with
                        JumpResults = jumpResult :: state.Results.JumpResults }

                let newStartlist = startlistMarkDone jumpFromDto.CompetitorId state.Startlist

                let newState =
                    { state with
                        Version = nextVersion
                        Results = newResults
                        Startlist = newStartlist }

                let stateAfterClearingCoachGateChange = state.clearCoachChange newState

                { stateAfterClearingCoachGateChange with
                    Version = nextVersion }

            | CompetitorDisqualifiedV1 e ->
                let newSL = startlistMarkDone e.CompetitorId state.Startlist

                { state with
                    Version = nextVersion
                    Startlist = newSL }

            | CompetitorDidNotStartV1 e ->
                let newSL = startlistMarkDone e.CompetitorId state.Startlist

                { state with
                    Version = nextVersion
                    Startlist = newSL }

            | GateChangedByJuryV1 e ->
                let gs = state.currentGateState ()
                let (Gate g) = gs.CurrentJury

                let updated =
                    { gs with
                        CurrentJury = Jump.Gate(g + e.Count) }

                state.withGateState updated

            | GateLoweredByCoachV1 e ->
                let gs = state.currentGateState ()

                let updated =
                    { gs with
                        CoachChange = Some(Reduction(uint e.Count)) }

                state.withGateState updated

            | StartingGateSetV1 e ->
                let gs =
                    { Starting = Jump.Gate e.Gate
                      CurrentJury = Jump.Gate e.Gate
                      CoachChange = None }

                state.withGateState gs

            | CompetitionSuspendedV1 _ ->
                match state.Status with
                | RoundInProgress(gs, r, g) ->
                    { state with
                        Version = nextVersion
                        Status = Suspended(gs, r, g) }
                | _ -> state

            | CompetitionContinuedV1 _ ->
                match state.Status with
                | Suspended(gs, r, g) ->
                    { state with
                        Version = nextVersion
                        Status = RoundInProgress(gs, r, g) }
                | _ -> state

            | CompetitionCancelledV1 _ ->
                { state with
                    Version = nextVersion
                    Status = Cancelled }

            | CompetitionEndedV1 _ ->
                { state with
                    Version = nextVersion
                    Status = Ended }

            // zdarzenia, które nie mutują stanu (GroupEnded, RoundEnded, …)
            | CompetitionRoundEndedV1 _
            | CompetitionGroupEndedV1 _
            | _ -> { state with Version = nextVersion }

    // ---------- fold ------------------------------------------------------

    let evolveFromEvents events =
        events |> List.fold (fun st ev -> Some(evolve st ev)) None
