namespace App.Domain.Competition

open System
open FsToolkit.ErrorHandling

module Competition =
    type Status =
        | NotStarted of GateState
        | RoundInProgress of GateState * RoundIndex
        | Suspended of GateState * RoundIndex
        | Cancelled
        | Ended

    type StatusTag =
        | NotStartedTag
        | RoundInProgressTag
        | SuspendedTag
        | CancelledTag
        | EndedTag

    type Error =
        | JumpersEmpty
        | TeamsEmpty
        | GroupSettingsMissing
        | TeamMemberCountsNotEqual of UniqueCounts: int
        | JumperNotNextInStartlist of NextShouldBe: Startlist.Entry * JumperId: JumperId
        | InvalidStatus of Current: StatusTag * Expected: StatusTag list
        | Internal of string
        | CoachGateIncrease
        | StartlistError of Error: Startlist.Error

open Competition

type CompetitionId = CompetitionId of System.Guid

type Competition =
    private
        { Id: CompetitionId
          Status: Status
          Settings: Settings
          Startlist: Startlist
          Results: Results
          Hill: Hill
          Jumpers: Jumper list }

    member this.Jumpers_ = this.Jumpers

    member this.Startlist_ = this.Startlist

    member this.GetStatusTag =
        match this.Status with
        | Status.NotStarted _ -> StatusTag.NotStartedTag
        | Status.RoundInProgress _ -> StatusTag.RoundInProgressTag
        | Status.Suspended _ -> StatusTag.SuspendedTag
        | Status.Cancelled -> StatusTag.CancelledTag
        | Status.Ended -> StatusTag.EndedTag

    member this.GateState =
        match this.Status with
        | Status.NotStarted gs
        | Status.RoundInProgress(gs, _)
        | Status.Suspended(gs, _) -> Some gs
        | _ -> None

    member private this.CurrentGateState() =
        match this.Status with
        | Status.NotStarted gs
        | Status.RoundInProgress(gs, _)
        | Status.Suspended(gs, _) -> gs
        | _ -> invalidOp "Gate state unavailable in this status."

    member private this.WithGateState(gateState: GateState) =
        let newStatus =
            match this.Status with
            | Status.NotStarted _ -> Status.NotStarted gateState
            | Status.RoundInProgress(_, round) -> Status.RoundInProgress(gateState, round)
            | Status.Suspended(_, round) -> Status.Suspended(gateState, round)
            | other -> other

        { this with Status = newStatus }

    static member Create
        (id: CompetitionId, settings: Settings, hill: Hill, jumpers: Jumper list, startingGate: Gate)
        : Result<Competition, Error> =
        if List.isEmpty jumpers then
            Error Error.JumpersEmpty
        else
            let jumperIds = jumpers |> List.map (fun j -> j.Id)

            result {
                let! startlist =
                    Startlist.CreateLinear jumperIds
                    |> Result.mapError (fun e -> Error.Internal(string e))

                let gateState =
                    { Starting = startingGate
                      CurrentJury = startingGate
                      CoachChange = None }

                return
                    { Id = id
                      Settings = settings
                      Hill = hill
                      Status = Status.NotStarted gateState
                      Startlist = startlist
                      Results = Results.Empty
                      Jumpers = jumpers }
            }

    member this.Classification = this.Results.FinalClassification

    member this.SetStartingGate(gate: Gate) =
        let newGateState =
            { Starting = gate
              CurrentJury = gate
              CoachChange = None }

        match this.Status with
        | Status.NotStarted _ -> Ok(this.WithGateState newGateState)
        | Status.RoundInProgress _ when this.Startlist.DoneEntries.IsEmpty -> Ok(this.WithGateState newGateState)
        | _ -> Error(Error.InvalidStatus(this.GetStatusTag, [ StatusTag.NotStartedTag; StatusTag.RoundInProgressTag ]))

    member this.ChangeGateByJury(change: GateChange) =
        match this.Status with
        | Status.NotStarted _
        | Status.RoundInProgress _
        | Status.Suspended _ ->
            let gs = this.CurrentGateState()
            let (Gate current) = gs.CurrentJury

            let newGate =
                match change with
                | Increase by -> Gate(current + int by)
                | Reduction by -> Gate(current - int by)

            Ok(this.WithGateState { gs with CurrentJury = newGate })
        | _ ->
            Error(
                Error.InvalidStatus(
                    this.GetStatusTag,
                    [ StatusTag.NotStartedTag
                      StatusTag.RoundInProgressTag
                      StatusTag.SuspendedTag ]
                )
            )

    member this.LowerGateByCoach(change: GateChange) =
        match change with
        | Increase _ -> Error Error.CoachGateIncrease
        | Reduction _ ->
            let gs = this.CurrentGateState()
            Ok(this.WithGateState { gs with CoachChange = Some change })

    member this.Suspend() =
        match this.Status with
        | Status.RoundInProgress(gs, round) ->
            Ok
                { this with
                    Status = Status.Suspended(gs, round) }
        | _ -> Error(Error.InvalidStatus(this.GetStatusTag, [ StatusTag.RoundInProgressTag ]))

    member this.Continue() =
        match this.Status with
        | Status.Suspended(gs, round) ->
            Ok
                { this with
                    Status = Status.RoundInProgress(gs, round) }
        | _ -> Error(Error.InvalidStatus(this.GetStatusTag, [ StatusTag.SuspendedTag ]))

    member this.Cancel() =
        match this.Status with
        | Status.Ended
        | Status.Cancelled -> Error(Error.InvalidStatus(this.GetStatusTag, []))
        | _ -> Ok { this with Status = Status.Cancelled }

    member this.NextJumper =
        this.Startlist.NextEntry |> Option.map (fun e -> this.FindJumper e.JumperId)

    member private this.ClearCoachChange() =
        let newStatus =
            match this.Status with
            | Status.NotStarted gs -> Status.NotStarted { gs with CoachChange = None }
            | Status.RoundInProgress(gs, r) -> Status.RoundInProgress({ gs with CoachChange = None }, r)
            | Status.Suspended(gs, r) -> Status.Suspended({ gs with CoachChange = None }, r)
            | s -> s

        { this with Status = newStatus }

    member this.AddJump(jumpResultId: JumpResultId, jump: Jump) =
        let jumperId = jump.JumperId

        let ensureNextIs jid =
            match this.Startlist.NextEntry with
            | Some next when next.JumperId = jid -> Ok()
            | Some next -> Error(Error.JumperNotNextInStartlist(next, jid))
            | None ->
                Error(Error.InvalidStatus(this.GetStatusTag, [ StatusTag.NotStartedTag; StatusTag.RoundInProgressTag ]))

        let execute roundIndex =
            let gs = this.CurrentGateState()
            let jumper = this.FindJumper jumperId

            let jr =
                JumpResultCreator.createFisJumpResult
                    jumpResultId
                    jump
                    jumper
                    this.Hill
                    roundIndex
                    gs.CurrentReal
                    gs.CoachChange
                    gs.Starting
                |> Result.mapError (fun fis -> Error.Internal(string fis))

            result {
                let! jumpResult = jr

                let! resultsAfter =
                    this.Results.AddJump(jumpResult, this.JumperExists)
                    |> Result.mapError (fun e -> Error.Internal(string e))

                let! startlistAfter =
                    if this.Startlist.RemainingEntries |> List.exists (fun e -> e.JumperId = jumperId) then
                        this.Startlist.MarkJumpDone jumperId
                        |> Result.mapError (fun e -> Error.Internal(string e))
                    else
                        Ok this.Startlist

                let baseStatus =
                    match this.Status with
                    | Status.NotStarted _ -> Status.RoundInProgress(gs, roundIndex)
                    | _ -> this.Status

                if not startlistAfter.RoundIsFinished then
                    let updated =
                        { this with
                            Results = resultsAfter
                            Startlist = startlistAfter
                            Status = baseStatus }

                    return updated.ClearCoachChange()
                elif this.IsLastRound roundIndex then
                    let updated =
                        { this with
                            Results = resultsAfter
                            Startlist = startlistAfter
                            Status = Status.Ended }

                    return updated.ClearCoachChange()
                else
                    let (RoundIndex i) = roundIndex
                    let nextRound = RoundIndex(i + 1u)
                    let nextStatus = Status.RoundInProgress(gs, nextRound)
                    // IMPORTANT: use startlistAfter (po oznaczeniu bieżącego skoku jako done)
                    let nextStartlist =
                        this.BuildNextRoundStartlist(startlistAfter, nextRound, resultsAfter)

                    if nextStartlist.IsError then
                        raise (Exception($"Next round startlist internal error: {nextStartlist}"))

                    let updated =
                        { this with
                            Results = resultsAfter
                            Startlist = nextStartlist |> Result.toOption |> Option.get
                            Status = nextStatus }

                    return updated.ClearCoachChange()
            }

        match this.Status with
        | Status.NotStarted _ ->
            // Require that the jumper is actually the next in the startlist
            ensureNextIs jumperId |> Result.bind (fun () -> execute (RoundIndex 0u))
        | Status.RoundInProgress(_, round) -> ensureNextIs jumperId |> Result.bind (fun () -> execute round)
        | Status.Suspended _
        | Status.Cancelled
        | Status.Ended -> Error(Error.InvalidStatus(this.GetStatusTag, []))

    member private this.BuildNextRoundStartlist
        (prevStartlist: Startlist, nextRound: RoundIndex, currentResults: Results)
        : Result<Startlist, Error> =
        let totalsSinceReset =
            currentResults.TotalsSinceReset this.Settings.PointsResets nextRound

        let ranked = totalsSinceReset |> Map.toList |> List.sortByDescending snd

        let bibValue jid =
            prevStartlist.BibOf jid
            |> Option.map Startlist.Bib.value
            |> Option.defaultWith (fun _ -> invalidOp "BIB not found")

        let roundSettings =
            this.Settings.RoundSettings[int (RoundIndexModule.value nextRound)]

        let applyLimit =
            match roundSettings.RoundLimit with
            | RoundLimit.NoneLimit -> id
            | RoundLimit.Soft(RoundLimitValue n) ->
                fun (lst: (JumperId * double) list) ->
                    if lst.Length <= n then
                        lst
                    else
                        let top, rest = lst |> List.splitAt n

                        match top |> List.tryLast with
                        | Some(_, cutPts) ->
                            let extras = rest |> List.takeWhile (fun (_, p) -> p = cutPts)
                            top @ extras
                        | None -> lst
            | RoundLimit.Exact(RoundLimitValue n) ->
                fun lst ->
                    if lst.Length <= n then
                        lst
                    else
                        let topN, rest = lst |> List.splitAt n

                        match topN |> List.tryLast with
                        | None -> lst
                        | Some(_, cutPts) ->
                            let tied =
                                (topN @ rest) |> List.filter (fun (_, pts) -> pts = cutPts) |> List.map fst

                            let resolved = tied |> List.sortByDescending bibValue
                            let occupied = topN |> List.filter (fun (_, pts) -> pts > cutPts)
                            let need = n - occupied.Length
                            let winners = resolved |> List.truncate need |> Set.ofList

                            occupied
                            @ ((topN @ rest)
                               |> List.filter (fun (jid, pts) -> pts = cutPts && winners.Contains jid))

        let limited = ranked |> applyLimit

        let ordered =
            let ids = limited |> List.map fst
            if roundSettings.SortStartlist then ids |> List.rev else ids

        let startlist = Startlist.WithOrder prevStartlist ordered

        match startlist with
        | Ok startlist -> Ok(startlist)
        | Error error -> Error(Error.StartlistError error)

    member private this.IsLastRound(round: RoundIndex) =
        let (RoundIndex i) = round
        i = uint this.Settings.RoundSettings.Length - 1u

    member private this.JumperExists(jid: JumperId) =
        this.Jumpers |> List.exists (fun j -> j.Id = jid)

    member private this.FindJumper(jid: JumperId) =
        this.Jumpers |> List.find (fun j -> j.Id = jid)

    member this.ClassificationResultOf(jumperId: JumperId) =
        this.Classification |> List.tryFind (fun result -> result.JumperId = jumperId)
