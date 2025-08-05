namespace App.Domain.SimpleCompetition

open App.Domain.Shared.AggregateVersion
open App.Domain.SimpleCompetition.Jump
open App.Domain.SimpleCompetition.JumpResult
open App.Domain.SimpleCompetition.Event

module Competition =
    type Type =
        | Individual
        | Team

    type Status =
        | NotStarted of GateState: GateState
        | RoundInProgress of GateState: GateState * RoundIndex: RoundIndex * GroupIndex: GroupIndex option
        | Suspended of GateState: GateState * RoundIndex: RoundIndex * GroupIndex: GroupIndex option
        | Cancelled
        | Ended

    type StatusTag =
        | NotStartedTag
        | RoundInProgressTag
        | SuspendedTag
        | CancelledTag
        | EndedTag

    type Error =
        | CompetitorsEmpty
        | TeamsEmpty
        | GroupSettingsMissing
        | TeamMemberCountsNotEqual of UniqueCounts: int
        | JumperNotNextInStartlist of NextOnStartlist: Startlist.CompetitorEntry * CompetitorId: Competitor.Id
        | InvalidStatus of Current: StatusTag * Expected: StatusTag list
        | Internal of Message: string

open Competition

type Competition =
    private
        { Id: CompetitionId
          Version: AggregateVersion
          Type: Competition.Type
          Status: Status
          Settings: Settings
          Startlist: Startlist
          Results: Results
          Hill: Hill
          Competitors: Competitor list option
          Teams: Team list option }

    member this.Id_ = this.Id
    member this.Version_ = this.Version
    member this.Type_ = this.Type
    member this.Status_ = this.Status
    member this.Hill_ = this.Hill
    member this.Startlist_ = this.Startlist
    member this.Results_ = this.Results

    static member CreateIndividual
        (id: CompetitionId, version, settings: Settings, hill: Hill, competitors: Competitor list, gateState)
        : Result<Competition, Competition.Error> =

        if List.isEmpty competitors then
            Error(Competition.Error.CompetitorsEmpty)
        else
            let rec buildEntries (competitors: Competitor list) order acc =
                match (competitors: Competitor list) with
                | [] -> Ok(List.rev acc)
                | competitor :: tail ->
                    match Startlist.Order.tryCreate order with
                    | Error _ -> Error(Competition.Error.Internal "Error when creating a Startlist.Order")
                    | Ok orderVo ->
                        let entry: Startlist.CompetitorEntry =
                            { CompetitorId = competitor.Id
                              TeamId = Option.None
                              Order = orderVo }

                        buildEntries tail (order + 1) (entry :: acc)

            match buildEntries competitors 1 [] with
            | Error e -> Error e
            | Ok entries ->
                let startlist = Startlist.Create entries

                match startlist with
                | Ok startlist ->

                    Ok
                        { Id = id
                          Version = version
                          Type = Competition.Individual
                          Settings = settings
                          Hill = hill
                          Status = Competition.NotStarted gateState
                          Startlist = startlist
                          Results = Results.Empty
                          Competitors = Some competitors
                          Teams = Option.None }
                | Error e -> Error(Competition.Error.Internal $"Error during creating a Startlist: {e}")

    static member CreateTeam
        (id: CompetitionId, version, settings: Settings, hill: Hill, teams: Team list, gateState)
        : Result<Competition, Competition.Error> =

        if List.isEmpty teams then
            Error Competition.Error.TeamsEmpty
        elif settings.RoundSettings |> List.exists (fun rs -> rs.GroupSettings.IsNone) then
            Error(Competition.Error.GroupSettingsMissing)
        else
            let counts = teams |> List.map (fun t -> t.Competitors.Length) |> Set.ofList

            if counts.Count <> 1 then
                Error(Competition.Error.TeamMemberCountsNotEqual counts.Count)
            else
                let teamCount = teams.Length
                let membersPerTeam = teams.Head.Competitors.Length

                let rec buildEntries groupIndex teamIndex acc =
                    if groupIndex >= membersPerTeam then
                        Ok(List.rev acc)
                    else if teamIndex >= teamCount then
                        buildEntries (groupIndex + 1) 0 acc
                    else
                        let team = teams[teamIndex]
                        let competitor = team.Competitors[groupIndex]
                        let globalOrder = acc.Length + 1

                        match Startlist.Order.tryCreate globalOrder with
                        | Error _ -> Error(Competition.Error.Internal "Error creating Startlist.Order")
                        | Ok orderVo ->
                            let entry: Startlist.CompetitorEntry =
                                { CompetitorId = competitor.Id
                                  TeamId = Some team.Id
                                  Order = orderVo }

                            buildEntries groupIndex (teamIndex + 1) (entry :: acc)

                match buildEntries 0 0 [] with
                | Error e -> Error e
                | Ok entries ->
                    match Startlist.Create entries with
                    | Error _ -> Error(Competition.Error.Internal "Error creating Startlist")
                    | Ok startlist ->
                        Ok
                            { Id = id
                              Version = version
                              Type = Competition.Team
                              Settings = settings
                              Hill = hill
                              Status = Competition.NotStarted gateState
                              Startlist = startlist
                              Results = Results.Empty
                              Competitors = Option.None
                              Teams = Some teams }

    member this.AddJump
        (jumpResultId: JumpResult.Id, competitorId: Competitor.Id, jump: Jump)
        : Result<Competition * CompetitionEventPayload list, Competition.Error> =
        match this.Status with
        | NotStarted gateState
        | RoundInProgress(gateState, _, _) ->
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
                            gateState.Starting
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
        | _ ->
            Error(
                Competition.Error.InvalidStatus(
                    this.StatusTag,
                    [ Competition.RoundInProgressTag; Competition.NotStartedTag ]
                )
            )

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
        | Competition.RoundInProgress(gateState, rnd, grp) ->
            let suspended =
                { this with
                    Status = Status.Suspended(gateState, rnd, grp) }

            let ev =
                CompetitionEventPayload.CompetitionSuspendedV1
                    { CompetitionId = this.Id
                      Reason = reason }

            Ok(suspended, [ ev ])
        | _ -> Error(Competition.Error.InvalidStatus(this.StatusTag, [ Competition.RoundInProgressTag ]))

    member this.Continue() : Result<Competition * CompetitionEventPayload list, Competition.Error> =
        match this.Status with
        | Competition.Suspended(gateState, rnd, grp) ->
            let continued =
                { this with
                    Status = Status.RoundInProgress(gateState, rnd, grp) }

            let ev = CompetitionEventPayload.CompetitionContinuedV1 { CompetitionId = this.Id }
            Ok(continued, [ ev ])
        | _ -> Error(Competition.Error.InvalidStatus(this.StatusTag, [ Competition.SuspendedTag ]))

    member this.Cancel(reason: string) : Result<Competition * CompetitionEventPayload list, Competition.Error> =
        match this.Status with
        | Competition.Ended
        | Competition.Cancelled -> Error(Competition.Error.InvalidStatus(this.StatusTag, []))
        | _ ->
            let cancelled = { this with Status = Status.Cancelled }

            let ev =
                CompetitionEventPayload.CompetitionCancelledV1
                    { CompetitionId = this.Id
                      Reason = reason }

            Ok(cancelled, [ ev ])

    member this.NextCompetitor =
        this.Startlist.NextJumper()
        |> Option.map (fun entry -> this.findCompetitor entry.CompetitorId)


    // ------- HELPERS START ------- //

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
                    |> Result.map (fun newStartlist -> newResults, newStartlist, maybeJumpResult)))
            |> Result.bind (fun (resultsAfter, startlistAfter, maybeJumpResult) ->
                let baseEvents = baseEventsFn maybeJumpResult

                let grpEvents, roundEvents, finalStatus, finalStartlist =
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
        | Competition.NotStarted gateState ->
            if not (this.competitorExists competitorId) then
                Error(Competition.Error.Internal $"Competitor {competitorId} not found in competition")
            else
                let firstRound = RoundIndex 0u

                let firstGroup =
                    if this.Type = Competition.Type.Team then
                        Some(GroupIndex 0u)
                    else
                        None

                let startEvents = this.startCompetitionEvents firstRound firstGroup

                proceed firstRound firstGroup
                |> Result.map (fun (comp, evs) -> comp, startEvents @ evs)
        | RoundInProgress(gateState, rnd, grp) ->
            match this.Startlist.NextJumper() with
            | Some nextEntry when nextEntry.CompetitorId <> competitorId ->
                Error(Competition.Error.JumperNotNextInStartlist(nextEntry, competitorId))
            | None ->
                if this.competitorExists competitorId then
                    proceed rnd grp
                else
                    Error(Competition.Error.Internal $"Competitor {competitorId} not found")
            | _ -> proceed rnd grp
        | Suspended _
        | Cancelled
        | Ended -> Error(Competition.Error.InvalidStatus(this.StatusTag, [ RoundInProgressTag ]))

    member private this.processProgression
        (updatedStartlist: Startlist)
        (roundIdx: RoundIndex)
        (groupIdx: GroupIndex option)
        : CompetitionEventPayload list * CompetitionEventPayload list * Status * Startlist =

        let gateState =
            match this.Status with
            | Status.RoundInProgress(gateState, _, _) -> gateState
            | _ -> failwith "Status is not RoundInProgress"


        let groupFinished = this.Type = Competition.Team && updatedStartlist.RoundIsFinished

        let baseStatus =
            match this.Status with
            | Competition.NotStarted gateState -> Status.RoundInProgress(gateState, roundIdx, groupIdx)
            | _ -> this.Status

        let groupEvents, statusAfterGroup, startlistAfterGroup =
            if groupFinished && this.Type = Competition.Team then
                let (GroupIndex g) = groupIdx.Value
                let totalGroups = uint this.membersPerTeam

                let groupEnded =
                    CompetitionEventPayload.CompetitionGroupEndedV1
                        { CompetitionId = this.Id
                          RoundIndex = roundIdx
                          GroupIndex = groupIdx.Value }

                let currentRoundSettings =
                    this.Settings.RoundSettings[int (RoundIndexModule.value roundIdx)]

                let nextGroupIndex = g + 1u

                if nextGroupIndex < totalGroups then
                    let nextGroup = GroupIndex nextGroupIndex

                    // teamMap: CompetitorId -> TeamId
                    let teamMap =
                        this.Teams.Value
                        |> List.collect (fun t -> t.Competitors |> List.map (fun c -> c.Id, t.Id))
                        |> Map.ofList

                    // liczymy punkty drużyn z uwzględnieniem ResetPoints
                    let lastResetRound =
                        this.Settings.RoundSettings
                        |> List.take (int (RoundIndexModule.value roundIdx + 1u))
                        |> List.rev
                        |> List.tryFindIndex (fun rs -> rs.ResetPoints)
                        |> Option.map uint

                    let teamPointsMap = this.calculateTeamPointsMap roundIdx

                    let mutable teamOrder =
                        match currentRoundSettings.GroupSettings with
                        | Some gs when gs.GroupIndexesToSort.Contains groupIdx.Value ->
                            // najgorsza drużyna pierwsza (FIS) – rosnąco po punktach
                            this.Teams.Value
                            |> List.sortBy (fun t -> Map.tryFind t.Id teamPointsMap |> Option.defaultValue 0.0)
                        | _ ->
                            // zostaw dotychczasową kolejność
                            this.Teams.Value

                    match currentRoundSettings.GroupSettings with
                    | Some gs when gs.GroupIndexesToSort.Contains groupIdx.Value ->
                        teamOrder <-
                            teamOrder
                            |> List.sortByDescending (fun t ->
                                Map.tryFind t.Id teamPointsMap |> Option.defaultValue 0.0)
                    | _ -> ()

                    let nextStartlist = this.buildTeamGroupStartlist (roundIdx, nextGroup)

                    let groupStarted =
                        CompetitionEventPayload.CompetitionGroupStartedV1
                            { CompetitionId = this.Id
                              RoundIndex = roundIdx
                              GroupIndex = nextGroup }

                    [ groupEnded; groupStarted ],
                    Status.RoundInProgress(gateState, roundIdx, Some nextGroup),
                    nextStartlist
                else
                    [ groupEnded ], Status.RoundInProgress(gateState, roundIdx, None), updatedStartlist
            else
                [], baseStatus, updatedStartlist

        let roundFinished =
            match this.Type with
            | Competition.Team ->
                match statusAfterGroup with
                | Competition.RoundInProgress(gateState, _, Some _) -> false
                | _ -> startlistAfterGroup.RoundIsFinished
            | _ -> startlistAfterGroup.RoundIsFinished

        let roundEvents, finalStatus, finalStartlist =
            if roundFinished then
                let roundEnded =
                    CompetitionEventPayload.CompetitionRoundEndedV1
                        { CompetitionId = this.Id
                          RoundIndex = roundIdx }

                if this.isLastRound roundIdx then
                    let ended = CompetitionEventPayload.CompetitionEndedV1 { CompetitionId = this.Id }
                    [ roundEnded; ended ], Status.Ended, startlistAfterGroup
                else
                    let nextRound = let (RoundIndex i) = roundIdx in RoundIndex(i + 1u)

                    let roundStarted =
                        CompetitionEventPayload.CompetitionRoundStartedV1
                            { CompetitionId = this.Id
                              RoundIndex = nextRound }

                    let settings = this.Settings.RoundSettings[int (RoundIndexModule.value nextRound)]

                    let nextStatus, nextStartlist =
                        if this.Type = Competition.Team then
                            Status.RoundInProgress(gateState, nextRound, Some(GroupIndex 0u)),
                            this.buildNextRoundStartlist (settings, nextRound, this.Results)
                        else
                            Status.RoundInProgress(gateState, nextRound, None),
                            this.buildNextRoundStartlist (settings, nextRound, this.Results)

                    [ roundEnded; roundStarted ], nextStatus, nextStartlist
            else
                [], statusAfterGroup, startlistAfterGroup

        groupEvents, roundEvents, finalStatus, finalStartlist

    member private this.buildNextRoundStartlist
        (roundSettings: RoundSettings, roundIdx: RoundIndex, currentResults: Results)
        : Startlist =

        match this.Type with
        | Competition.Team ->
            let teamMap =
                this.Teams.Value
                |> List.collect (fun t -> t.Competitors |> List.map (fun c -> c.Id, t.Id))
                |> Map.ofList

            let lastReset =
                this.Settings.RoundSettings
                |> List.take (int (RoundIndexModule.value roundIdx + 1u))
                |> List.rev
                |> List.tryFindIndex (fun rs -> rs.ResetPoints)
                |> Option.map uint

            let teamPointsMap =
                currentResults.JumpResults
                |> List.filter (fun jr ->
                    match lastReset with
                    | Some off -> let (RoundIndex i) = jr.RoundIndex in i > off
                    | None -> true)
                |> List.groupBy (fun jr -> Map.find jr.CompetitorId teamMap)
                |> Map.ofList
                |> Map.map (fun _ js -> js |> List.sumBy (fun j -> let (TotalPoints p) = j.TotalPoints in p))

            let rankedTeams =
                teamPointsMap
                |> Map.toList
                |> List.map (fun (tid, pts) ->
                    let teamJumps =
                        currentResults.JumpResults
                        |> List.filter (fun jr -> Map.find jr.CompetitorId teamMap = tid)

                    let bestDist =
                        teamJumps
                        |> List.maxBy _.Jump.Distance
                        |> fun jr -> jr.Jump.Distance |> Distance.value

                    let bestJudge =
                        teamJumps
                        |> List.maxBy (fun jr -> JudgeNotes.value jr.Jump.JudgeNotes |> List.sum)
                        |> fun jr -> JudgeNotes.value jr.Jump.JudgeNotes |> List.sum

                    tid, pts, bestDist, bestJudge)
                |> List.sortByDescending (fun (_, pts, _, _) -> pts)

            let advancedTeams, droppedTeams =
                match roundSettings.RoundLimit with
                | RoundLimit.NoneLimit -> rankedTeams, []

                | RoundLimit.Soft(RoundLimitValue n) ->
                    let topN, rest = rankedTeams |> List.splitAt n

                    let lastScoreOpt =
                        topN |> List.tryLast |> Option.map (fun (_, score, _, _) -> score)

                    let advanced =
                        match lastScoreOpt with
                        | Some lastScore ->
                            let extras = rest |> List.takeWhile (fun (_, s, _, _) -> s = lastScore)
                            topN @ extras
                        | None -> rankedTeams

                    let dropped = rankedTeams |> List.except advanced
                    advanced, dropped

                | RoundLimit.Exact(RoundLimitValue n, criteria) ->
                    let topN, rest = rankedTeams |> List.splitAt n

                    let lastScoreOpt =
                        topN |> List.tryLast |> Option.map (fun (_, score, _, _) -> score)

                    let advanced, dropped =
                        match lastScoreOpt with
                        | Some lastScore ->
                            let ties = topN |> List.filter (fun (_, s, _, _) -> s = lastScore)

                            if List.isEmpty ties then
                                topN, rest
                            else
                                let candidates = ties @ rest

                                let resolved =
                                    match criteria with
                                    | TieBreakerCriteria.LongestJump ->
                                        candidates |> List.sortByDescending (fun (_, _, dist, _) -> dist)
                                    | TieBreakerCriteria.BestJudgePoints ->
                                        candidates |> List.sortByDescending (fun (_, _, _, j) -> j)
                                    | TieBreakerCriteria.HighestBib ->
                                        candidates |> List.sortByDescending (fun (tid, _, _, _) -> tid)
                                    | TieBreakerCriteria.LowestBib ->
                                        candidates |> List.sortBy (fun (tid, _, _, _) -> tid)
                                    | TieBreakerCriteria.Random -> candidates

                                let keep =
                                    (topN |> List.take (n - List.length ties))
                                    @ (resolved |> List.take (List.length ties))

                                let drop = resolved |> List.skip (List.length ties)
                                keep, drop
                        | None -> rankedTeams, []

                    advanced, dropped


            let orderedTeams =
                if roundSettings.SortStartlist then
                    advancedTeams |> List.rev
                else
                    advancedTeams

            let members = this.membersPerTeam

            let entries =
                [ for grp in 0 .. members - 1 do
                      for idx, (tid, _, _, _) in orderedTeams |> List.indexed do
                          let comp =
                              this.Teams.Value |> List.find (fun t -> t.Id = tid) |> _.Competitors.[grp]

                          let order =
                              Startlist.Order.tryCreate (idx * members + grp + 1)
                              |> Result.toOption
                              |> Option.get

                          { CompetitorId = comp.Id
                            TeamId = Some tid
                            Order = order }
                          : Startlist.CompetitorEntry ]

            Startlist.Create entries |> Result.toOption |> Option.get

        | Competition.Individual ->
            let pointsMap =
                let lastReset =
                    this.Settings.RoundSettings
                    |> List.take (int (RoundIndexModule.value roundIdx + 1u))
                    |> List.rev
                    |> List.tryFindIndex (fun rs -> rs.ResetPoints)
                    |> Option.map uint

                currentResults.JumpResults
                |> List.filter (fun jr ->
                    match lastReset with
                    | Some off -> let (RoundIndex i) = jr.RoundIndex in i > off
                    | None -> true)
                |> List.groupBy (fun jr -> jr.CompetitorId)
                |> Map.ofList
                |> Map.map (fun _ js -> js |> List.sumBy (fun j -> let (TotalPoints p) = j.TotalPoints in p))

            let prevRound =
                let (RoundIndex i) = roundIdx
                RoundIndex(i - 1u)

            let ranked =
                this.roundClassification currentResults prevRound
                |> List.map (fun (cid, _, dist, judge) ->
                    let pts = Map.tryFind cid pointsMap |> Option.defaultValue 0.0
                    cid, pts, dist, judge)
                |> List.sortByDescending (fun (_, pts, _, _) -> pts)

            // let ranked =
            //     this.roundClassification currentResults roundIdx
            //     |> List.map (fun (cid, _, dist, judge) ->
            //         let pts = Map.tryFind cid pointsMap |> Option.defaultValue 0.0
            //         cid, pts, dist, judge)
            //     |> List.sortByDescending (fun (_, pts, _, _) -> pts)

            let advanced, _ =
                match roundSettings.RoundLimit with
                | RoundLimit.NoneLimit -> ranked, []
                | RoundLimit.Soft(RoundLimitValue n) ->
                    let split = ranked |> List.splitAt n
                    let lastPts = split |> fst |> List.tryLast |> Option.map (fun (_, p, _, _) -> p)

                    match lastPts with
                    | Some p ->
                        let extra = split |> snd |> List.takeWhile (fun (_, q, _, _) -> q = p)
                        fst split @ extra, snd split |> List.skip (List.length extra)
                    | None -> ranked, []
                | RoundLimit.Exact(RoundLimitValue n, crit) ->
                    let firstN, tails = ranked |> List.splitAt n

                    let ties =
                        match firstN |> List.tryLast with
                        | Some(_, p, _, _) -> firstN |> List.filter (fun (_, q, _, _) -> q = p)
                        | None -> []

                    if ties.IsEmpty then
                        firstN, tails
                    else
                        let resolved = ties @ tails |> this.applyTieBreaker crit

                        (firstN |> List.take (n - ties.Length)) @ (resolved |> List.take ties.Length),
                        resolved |> List.skip ties.Length

            let ordered =
                if roundSettings.SortStartlist then
                    advanced |> List.rev
                else
                    advanced

            let entries =
                ordered
                |> List.mapi (fun i (cid, _, _, _) ->
                    let competitor = this.findCompetitor cid
                    let order = Startlist.Order.tryCreate (i + 1) |> Result.toOption |> Option.get

                    { CompetitorId = cid
                      TeamId = competitor.TeamId
                      Order = order }
                    : Startlist.CompetitorEntry)

            Startlist.Create entries |> Result.toOption |> Option.get


    // ... reszta helperów (findCompetitor, competitorExists, roundClassification,
    //     applyTieBreaker, isLastRound, toJumpDto, buildTeamGroupStartlist,
    //     startCompetitionEvents, resultsErrorToInternal, membersPerTeam) bez zmian.
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
        current = uint this.Settings.RoundSettings.Length - 1u

    member private this.toJumpDto(jumpResult: JumpResult) : Event.JumpDtoV1 =
        { Id = jumpResult.Jump.Id
          CompetitorId = jumpResult.CompetitorId
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

    member private this.calculateTeamPointsMap(currentRoundIdx: RoundIndex) =
        let teamMap =
            this.Teams.Value
            |> List.collect (fun t -> t.Competitors |> List.map (fun c -> c.Id, t.Id))
            |> Map.ofList

        let lastReset =
            this.Settings.RoundSettings
            |> List.take (int (RoundIndexModule.value currentRoundIdx + 1u))
            |> List.rev
            |> List.tryFindIndex (fun rs -> rs.ResetPoints)
            |> Option.map uint

        this.Results.JumpResults
        |> List.filter (fun jr ->
            match lastReset with
            | Some off -> let (RoundIndex i) = jr.RoundIndex in i > off
            | None -> true)
        |> List.groupBy (fun jr -> Map.find jr.CompetitorId teamMap)
        |> Map.ofList
        |> Map.map (fun _ js -> js |> List.sumBy (fun j -> let (TotalPoints p) = j.TotalPoints in p))


    member private this.buildTeamGroupStartlist(roundIdx: RoundIndex, groupIdx: GroupIndex) =
        let teamPointsMap = this.calculateTeamPointsMap roundIdx

        let roundSettings =
            this.Settings.RoundSettings[int (RoundIndexModule.value roundIdx)]

        let teamsOrdered =
            match roundSettings.GroupSettings with
            | Some gs when gs.GroupIndexesToSort.Contains groupIdx ->
                this.Teams.Value
                |> List.sortBy (fun t -> Map.tryFind t.Id teamPointsMap |> Option.defaultValue 0.0)
            | _ -> this.Teams.Value

        let idxInt = int (GroupIndexModule.value groupIdx)

        if idxInt >= this.membersPerTeam then
            invalidOp "Group index out of range"

        let entries =
            teamsOrdered
            |> List.mapi (fun idx team ->
                let competitor = team.Competitors[int (GroupIndexModule.value groupIdx)]
                let order = Startlist.Order.tryCreate (idx + 1) |> Result.toOption |> Option.get

                { CompetitorId = competitor.Id
                  TeamId = Some team.Id
                  Order = order }
                : Startlist.CompetitorEntry)

        Startlist.Create entries |> Result.toOption |> Option.get

    member private this.resultsErrorToInternal(e: Results.Error) =
        Competition.Error.Internal(e.ToString())

    member private this.startCompetitionEvents firstRound firstGroup =
        let started =
            CompetitionEventPayload.CompetitionStartedV1 { CompetitionId = this.Id }

        let roundEvt =
            CompetitionEventPayload.CompetitionRoundStartedV1
                { CompetitionId = this.Id
                  RoundIndex = firstRound }

        match this.Type with
        | Competition.Team ->
            let groupEvt =
                CompetitionEventPayload.CompetitionGroupStartedV1
                    { CompetitionId = this.Id
                      RoundIndex = firstRound
                      GroupIndex = firstGroup.Value }

            [ started; roundEvt; groupEvt ]
        | _ -> [ started; roundEvt ]

    // ======================  RANKING  ===============================

    /// klasyfikacja rundy; zwraca listę (CompetitorId * TotalPts * BestDistance * BestJudgeSum)
    member private this.roundClassification (results: Results) (roundIdx: RoundIndex) =
        results.JumpResults
        |> List.filter (fun jr -> jr.RoundIndex = roundIdx)
        |> List.groupBy (fun jr -> jr.CompetitorId)
        |> List.map (fun (cid, jumps) ->
            let total =
                jumps |> List.sumBy (fun jr -> let (TotalPoints p) = jr.TotalPoints in p)

            let bestDist =
                jumps
                |> List.maxBy (fun jr -> jr.Jump.Distance)
                |> fun jr -> jr.Jump.Distance |> Distance.value

            let bestJudge =
                jumps
                |> List.maxBy (fun jr -> JudgeNotes.value jr.Jump.JudgeNotes |> List.sum)
                |> fun jr -> JudgeNotes.value jr.Jump.JudgeNotes |> List.sum

            cid, total, bestDist, bestJudge) // bestDist = float
        |> List.sortByDescending (fun (_, pts, _, _) -> pts)

    member private this.applyTieBreaker
        (criteria: TieBreakerCriteria)
        (ties: (Competitor.Id * float * float * float) list)
        =
        match criteria with
        | TieBreakerCriteria.LongestJump -> ties |> List.sortByDescending (fun (_, _, dist, _) -> dist)
        | TieBreakerCriteria.BestJudgePoints -> ties |> List.sortByDescending (fun (_, _, _, judge) -> judge)
        | TieBreakerCriteria.HighestBib -> ties |> List.sortByDescending (fun (cid, _, _, _) -> cid) // Id ma w sobie BIB
        | TieBreakerCriteria.LowestBib -> ties |> List.sortBy (fun (cid, _, _, _) -> cid)
        | Random -> failwith "todo"

    // ======================  Other Helpers  ===============================

    member private this.StatusTag =
        match this.Status with
        | NotStarted _ -> NotStartedTag
        | RoundInProgress _ -> RoundInProgressTag
        | Suspended _ -> SuspendedTag
        | Cancelled -> CancelledTag
        | Ended -> EndedTag
