namespace App.Domain.SimpleCompetition

open App.Domain.SimpleCompetition.Jump
open App.Domain.SimpleCompetition.JumpResult
open App.Domain.SimpleCompetition.Event

module Competition =
    type Type =
        | Individual
        | Team

    type Status =
        | NotStarted
        | RoundInProgress of RoundIndex: RoundIndex * GroupIndex: GroupIndex option
        | Suspended of RoundIndex: RoundIndex * GroupIndex: GroupIndex option
        | Cancelled
        | Ended

    type Error =
        | CompetitorsEmpty
        | TeamsEmpty
        | GroupSettingsMissing
        | TeamMemberCountsNotEqual of UniqueCounts: int
        | JumperNotNextInStartlist of NextOnStartlist: Startlist.CompetitorEntry * CompetitorId: Competitor.Id
        | InvalidStatus of Current: Status * Expected: Status list
        | Internal of Message: string

type Competition =
    private
        { Id: CompetitionId
          Type: Competition.Type
          Status: Competition.Status
          Settings: Settings
          Startlist: Startlist
          Results: Results
          Hill: Hill
          Competitors: Competitor list option
          Teams: Team list option }

    // --------------------------------------------------------------------------------
    //  ------------------------------  PUBLIC API  -----------------------------------
    // --------------------------------------------------------------------------------

    member this.AddJump
        (jumpResultId: JumpResult.Id, competitorId: Competitor.Id, jump: Jump, referenceGate: Jump.Gate)
        : Result<Competition * CompetitionEventPayload list, Competition.Error> =
        this.handleAction
            competitorId
            (fun roundIdx groupIdx ->
                let competitor = this.findCompetitor competitorId

                match
                    JumpResultCreator.createFisJumpResult
                        jumpResultId
                        jump
                        competitor
                        this.Hill
                        roundIdx
                        groupIdx
                        referenceGate
                with
                | Ok jumpResult -> Ok(Some jumpResult)
                | Error fisErr ->
                    let msg = fisErr.ToString()
                    Error(Competition.Error.Internal msg))
            (fun jumpResult ->
                match jumpResult with
                | Some jr ->
                    CompetitionEventPayload.JumpAddedV1
                        { CompetitionId = this.Id
                          Jump = this.toJumpDto jr }
                    |> List.singleton
                | None -> [])


    member this.Disqualify
        (competitorId: Competitor.Id, reason: DisqualificationReason)
        : Result<Competition * CompetitionEventPayload list, Competition.Error> =
        this.handleAction competitorId (fun _ _ -> Ok None) (fun _ ->
            CompetitionEventPayload.CompetitorDisqualifiedV1
                { CompetitionId = this.Id
                  CompetitorId = competitorId
                  Reason = reason }
            |> List.singleton)

    member this.MarkAsDidNotStart
        (competitorId: Competitor.Id)
        : Result<Competition * CompetitionEventPayload list, Competition.Error> =
        this.handleAction competitorId (fun _ _ -> Ok None) (fun _ ->
            CompetitionEventPayload.CompetitorDidNotStartV1
                { CompetitionId = this.Id
                  CompetitorId = competitorId }
            |> List.singleton)

    member this.Suspend(reason: string) : Result<Competition * CompetitionEventPayload list, Competition.Error> =
        match this.Status with
        | Competition.RoundInProgress(rnd, grp) ->
            let suspended =
                { this with
                    Status = Competition.Status.Suspended(rnd, grp) }

            let ev =
                CompetitionEventPayload.CompetitionSuspendedV1
                    { CompetitionId = this.Id
                      Reason = reason }

            Ok(suspended, [ ev ])
        | _ ->
            Error(
                Competition.Error.InvalidStatus(
                    this.Status,
                    [ Competition.Status.RoundInProgress(RoundIndex 0u, None) ]
                )
            )

    member this.Continue() : Result<Competition * CompetitionEventPayload list, Competition.Error> =
        match this.Status with
        | Competition.Suspended(rnd, grp) ->
            let continued =
                { this with
                    Status = Competition.Status.RoundInProgress(rnd, grp) }

            let ev = CompetitionEventPayload.CompetitionContinuedV1 { CompetitionId = this.Id }

            Ok(continued, [ ev ])
        | _ ->
            Error(Competition.Error.InvalidStatus(this.Status, [ Competition.Status.Suspended(RoundIndex 0u, None) ]))

    member this.Cancel(reason: string) : Result<Competition * CompetitionEventPayload list, Competition.Error> =
        match this.Status with
        | Competition.Ended
        | Competition.Cancelled -> Error(Competition.Error.InvalidStatus(this.Status, []))
        | _ ->
            let cancelled =
                { this with
                    Status = Competition.Status.Cancelled }

            let ev =
                CompetitionEventPayload.CompetitionCancelledV1
                    { CompetitionId = this.Id
                      Reason = reason }

            Ok(cancelled, [ ev ])

    // --------------------------------------------------------------------------------
    //  -----------  WSPÓLNA ŚCIEŻKA DLA ADDJUMP / DSQ / DNS  -------------------------
    // --------------------------------------------------------------------------------

    member private this.handleAction
        (competitorId: Competitor.Id)
        (domainAction: RoundIndex -> GroupIndex option -> Result<JumpResult option, Competition.Error>)
        (baseEventsFn: JumpResult option -> CompetitionEventPayload list)
        : Result<Competition * CompetitionEventPayload list, Competition.Error> =

        let proceed roundIdx groupIdx =
            domainAction roundIdx groupIdx
            |> Result.bind (fun maybeJumpResult ->

                let resultsAfter =
                    match maybeJumpResult with
                    | Some jr ->
                        match this.Results.AddJump(jr, this.competitorExists) with
                        | Ok r -> Ok r
                        | Error e -> Error(this.resultsErrorToInternal e)
                    | None -> Ok this.Results

                resultsAfter
                |> Result.bind (fun newResults ->

                    let startlistAfterAction =
                        if this.Startlist.Remaining |> List.exists (fun e -> e.CompetitorId = competitorId) then
                            match this.Startlist.MarkJumpDone competitorId with
                            | Ok sl -> Ok sl
                            | Error e -> Error(Competition.Error.Internal $"Startlist error: {e}")
                        else
                            Ok this.Startlist

                    startlistAfterAction
                    |> Result.map (fun newStartlist -> newResults, newStartlist, maybeJumpResult))

            )
            |> Result.bind (fun (resultsAfter, startlistAfter, maybeJumpResult) ->
                let baseEvents = baseEventsFn maybeJumpResult

                let (grpEvents, roundEvents, finalStatus, finalStartlist) =
                    this.processProgression startlistAfter roundIdx groupIdx

                let allEvents = baseEvents @ grpEvents @ roundEvents

                Ok(
                    { this with
                        Results = resultsAfter
                        Startlist = finalStartlist
                        Status = finalStatus },
                    allEvents
                ))

        match this.Status with
        | Competition.NotStarted ->
            if not (this.competitorExists competitorId) then
                Error(Competition.Error.Internal $"Competitor {competitorId} not found in competition")
            else
                let firstRound = RoundIndex 0u

                let firstGroup =
                    if this.Type = Competition.Team then
                        Some(GroupIndex 0u)
                    else
                        None

                let startEvents = this.startCompetitionEvents firstRound firstGroup

                proceed firstRound firstGroup
                |> Result.map (fun (comp, evs) -> comp, startEvents @ evs)

        | Competition.RoundInProgress(rnd, grp) ->
            match this.Startlist.NextJumper() with
            | Some nextEntry when nextEntry.CompetitorId <> competitorId ->
                Error(Competition.Error.JumperNotNextInStartlist(nextEntry, competitorId))
            | _ when not (this.competitorExists competitorId) ->
                Error(Competition.Error.Internal $"Competitor {competitorId} not found in competition")
            | _ -> proceed rnd grp

        | Competition.Suspended _
        | Competition.Cancelled
        | Competition.Ended ->
            Error(
                Competition.Error.InvalidStatus(
                    this.Status,
                    [ Competition.Status.RoundInProgress(RoundIndex 0u, None) ]
                )
            )

    // --------------------------------------------------------------------------------
    //  -----------------  LOGIKA GRUP / RUND / KOŃCA KONKURSU  -----------------------
    // --------------------------------------------------------------------------------

    member private this.processProgression
        (updatedStartlist: Startlist)
        (roundIdx: RoundIndex)
        (groupIdx: GroupIndex option)
        : CompetitionEventPayload list * CompetitionEventPayload list * Competition.Status * Startlist =

        // --- zakończenie grupy (tylko drużynówka) ---
        let groupFinished = this.Type = Competition.Team && updatedStartlist.RoundIsFinished

        let groupEvents, statusAfterGroup, startlistAfterGroup =
            if groupFinished then
                let (GroupIndex g) = groupIdx.Value
                let totalGroups = uint this.membersPerTeam

                let groupEnded =
                    CompetitionEventPayload.CompetitionGroupEndedV1
                        { CompetitionId = this.Id
                          RoundIndex = roundIdx
                          GroupIndex = groupIdx.Value }

                if g + 1u < totalGroups then
                    let nextGroup = GroupIndex(g + 1u)
                    let nextStart = this.buildTeamGroupStartlist nextGroup

                    let groupStarted =
                        CompetitionEventPayload.CompetitionGroupStartedV1
                            { CompetitionId = this.Id
                              RoundIndex = roundIdx
                              GroupIndex = nextGroup }

                    [ groupEnded; groupStarted ],
                    Competition.Status.RoundInProgress(roundIdx, Some nextGroup),
                    nextStart
                else
                    [ groupEnded ], Competition.Status.RoundInProgress(roundIdx, None), updatedStartlist
            else
                [], this.Status, updatedStartlist

        // --- zakończenie rundy ---
        let roundFinished =
            match this.Type with
            | Competition.Team ->
                match statusAfterGroup with
                | Competition.RoundInProgress(_, Some _) -> false
                | _ -> startlistAfterGroup.RoundIsFinished
            | _ -> startlistAfterGroup.RoundIsFinished

        let roundEvents, finalStatus =
            if roundFinished then
                let roundEnded =
                    CompetitionEventPayload.CompetitionRoundEndedV1
                        { CompetitionId = this.Id
                          RoundIndex = roundIdx }

                if this.isLastRound roundIdx then
                    let ended = CompetitionEventPayload.CompetitionEndedV1 { CompetitionId = this.Id }

                    [ roundEnded; ended ], Competition.Status.Ended
                else
                    let nextRound = let (RoundIndex i) = roundIdx in RoundIndex(i + 1u)

                    let roundStarted =
                        CompetitionEventPayload.CompetitionRoundStartedV1
                            { CompetitionId = this.Id
                              RoundIndex = nextRound }

                    let nextStatus =
                        if this.Type = Competition.Team then
                            Competition.Status.RoundInProgress(nextRound, Some(GroupIndex 0u))
                        else
                            Competition.Status.RoundInProgress(nextRound, None)

                    [ roundEnded; roundStarted ], nextStatus
            else
                [], statusAfterGroup

        groupEvents, roundEvents, finalStatus, startlistAfterGroup

    // --------------------------------------------------------------------------------
    //  -------------------  RESZTA HELPERÓW (bez zmian)  ------------------------------
    // --------------------------------------------------------------------------------
    // ... (findCompetitor, competitorExists, isLastRound, toJumpDto, buildTeamGroupStartlist,
    //      startCompetitionEvents, resultsErrorToInternal, membersPerTeam, etc.)

    member private this.competitorExists(competitorId: Competitor.Id) =
        match this.Type, this.Competitors, this.Teams with
        | Competition.Individual, Some competitors, _ -> competitors |> List.exists (fun c -> c.Id = competitorId)
        | Competition.Team, _, Some teams ->
            teams
            |> List.exists (fun t -> t.Competitors |> List.exists (fun c -> c.Id = competitorId))
        | _ -> false

    member private this.findCompetitor(competitorId: Competitor.Id) =
        match this.Type, this.Competitors, this.Teams with
        | Competition.Individual, Some competitors, _ -> competitors |> List.find (fun c -> c.Id = competitorId)
        | Competition.Team, _, Some teams ->
            teams |> List.collect _.Competitors |> List.find (fun c -> c.Id = competitorId)
        | _ -> failwith "Competitor not found"

    member private this.isLastRound(roundIndex: RoundIndex) =
        let (RoundIndex current) = roundIndex
        current = uint this.Settings.RoundSettings.Length

    member private this.toJumpDto(jumpResult: JumpResult) : Event.JumpDtoV1 =
        { Id = jumpResult.Jump.Id
          Distance = TotalPointsModule.value jumpResult.TotalPoints
          Gate = GateModule.value jumpResult.Jump.Gate
          GatesLoweredByCoach = GatesLoweredByCoachModule.value jumpResult.Jump.GatesLoweredByCoach
          WindAverage = jumpResult.Jump.WindAverage.ToDouble()
          JudgeNotes = JudgeNotes.value jumpResult.Jump.JudgeNotes }

    member private this.teamCount =
        match this.Type, this.Teams with
        | Competition.Team, Some ts -> ts.Length
        | _ -> 0

    member private this.membersPerTeam =
        match this.Type, this.Teams with
        | Competition.Team, Some ts -> ts.Head.Competitors.Length
        | _ -> 0

    member private this.buildTeamGroupStartlist(groupIdx: GroupIndex) =
        let teams = this.Teams.Value

        let order =
            teams
            |> List.collect (fun t -> [ t.Competitors[int (GroupIndexModule.value groupIdx)] ])
            |> List.mapi (fun i competitor ->
                match Startlist.Order.tryCreate (i + 1) with
                | Ok orderOnStartlist ->
                    { CompetitorId = competitor.Id
                      TeamId = Some teams[i].Id
                      Order = orderOnStartlist }
                    : Startlist.CompetitorEntry
                | Error _ -> invalidOp "Internal error")

        let startlistResult = Startlist.Create order

        match startlistResult with
        | Ok startlist -> startlist
        | Error error -> invalidOp (error.ToString())

    member private this.resultsErrorToInternal(e: Results.Error) =
        Competition.Error.Internal(e.ToString())

    member private this.startCompetitionEvents firstRound firstGroup =
        let started =
            Event.CompetitionEventPayload.CompetitionStartedV1 { CompetitionId = this.Id }

        let roundEvt =
            Event.CompetitionEventPayload.CompetitionRoundStartedV1
                { CompetitionId = this.Id
                  RoundIndex = firstRound }

        match this.Type with
        | Competition.Team ->
            let groupEvt =
                Event.CompetitionEventPayload.CompetitionGroupStartedV1
                    { CompetitionId = this.Id
                      RoundIndex = firstRound
                      GroupIndex = firstGroup.Value }

            [ started; roundEvt; groupEvt ]
        | _ -> [ started; roundEvt ]
