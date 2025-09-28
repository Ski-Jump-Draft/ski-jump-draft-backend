namespace App.Domain.Competition

open System
open FsToolkit.ErrorHandling

module Competition =
    type Status =
        | NotStarted of GateState: GateState
        | RoundInProgress of GateState: GateState * RoundIndex: RoundIndex
        | Suspended of GateState: GateState * RoundIndex: RoundIndex
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

[<RequireQualifiedAccess>]
type CompetitionResume =
    {
        Id: CompetitionId
        Settings: Settings
        Hill: Hill
        Jumpers: Jumper list
        /// Stabilna mapa BIBów (całej konkurencji), zwykle z DB.
        Bibs: (JumperId * Startlist.Bib) list
        /// Uporządkowanie w AKTUALNEJ rundzie: zrobione w tej rundzie (w kolejności wykonania).
        DoneOrder: JumperId list
        /// Uporządkowanie w AKTUALNEJ rundzie: oczekujący (w kolejności startu).
        RemainingOrder: JumperId list
        /// Wszystkie dotychczasowe wyniki skoków (dowolnie ułożone).
        JumpResults: JumpResult list
        /// Jaki status ma być przywrócony.
        StatusTag: Competition.StatusTag
        /// Wymagane dla: NotStarted, RoundInProgress, Suspended
        GateState: GateState option
        /// Wymagane dla: RoundInProgress, Suspended
        RoundIndex: RoundIndex option
    }

module private CompetitionRestoreHelpers =
    let inline mapStartlistErr e = Competition.Error.StartlistError e
    let inline internalErr msg = Competition.Error.Internal msg

    let markManyDone (ids: JumperId list) (sl: Startlist) =
        (Ok sl, ids)
        ||> List.fold (fun acc jid ->
            acc
            |> Result.bind (fun s -> s.MarkJumpDone jid |> Result.mapError mapStartlistErr))

type Competition =
    private
        { Id: CompetitionId
          Status: Status
          Settings: Settings
          Startlist: Startlist
          Results: Results
          Hill: Hill
          Jumpers: Jumper list }

    member this.Id_ = this.Id

    member this.Jumpers_ = this.Jumpers

    member this.Startlist_ = this.Startlist

    member this.GetStatusTag =
        match this.Status with
        | Status.NotStarted _ -> StatusTag.NotStartedTag
        | Status.RoundInProgress _ -> StatusTag.RoundInProgressTag
        | Status.Suspended _ -> StatusTag.SuspendedTag
        | Status.Cancelled -> StatusTag.CancelledTag
        | Status.Ended -> StatusTag.EndedTag

    member this.Status_ = this.Status

    member this.CurrentRoundIndex: RoundIndex option =
        match this.Status with
        | Status.RoundInProgress(_, roundIndex) -> Some roundIndex
        | _ -> None

    member this.GateState =
        match this.Status with
        | Status.NotStarted gs
        | Status.RoundInProgress(gs, _)
        | Status.Suspended(gs, _) -> Some gs
        | _ -> None

    member private this.CurrentGateState() : GateState =
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

    static member Restore(snap: CompetitionResume) : Result<Competition, Competition.Error> =
        result {
            // 0) Proste sprawdzenia wejścia
            do!
                if List.isEmpty snap.Jumpers then
                    Error Competition.Error.JumpersEmpty
                else
                    Ok()

            let jumperSet = snap.Jumpers |> List.map (fun j -> j.Id) |> Set.ofList

            do!
                if snap.Bibs |> List.forall (fun (jid, _) -> jumperSet.Contains jid) then
                    Ok()
                else
                    Error(CompetitionRestoreHelpers.internalErr "BIB map contains unknown JumperId")

            // 1) Zbuduj bazowy Startlist z mapy BIB
            let! baseStartlist =
                Startlist.CreateWithBibs snap.Bibs
                |> Result.mapError CompetitionRestoreHelpers.mapStartlistErr

            // 2) Ustaw kolejność bieżącej rundy: done ++ remaining
            let roundOrder = snap.DoneOrder @ snap.RemainingOrder

            do!
                // duplicates/unknowny złapie WithOrder, ale sprawdźmy też spójność
                if roundOrder |> Set.ofList |> Set.isSubset jumperSet then
                    Ok()
                else
                    Error(CompetitionRestoreHelpers.internalErr "Round order contains unknown JumperId")

            let! orderedStartlist =
                Startlist.WithOrder baseStartlist roundOrder
                |> Result.mapError CompetitionRestoreHelpers.mapStartlistErr

            // 3) Przewiń startlistę o już wykonane skoki (tej rundy)
            let! startlistAfterProgress = CompetitionRestoreHelpers.markManyDone snap.DoneOrder orderedStartlist

            // 4) Odtwórz Results, walidując spójność (brak duplikatów w rundzie, istniejący zawodnicy)
            let competExists (jid: JumperId) = jumperSet.Contains jid

            let! results =
                (Ok Results.Empty, snap.JumpResults)
                ||> List.fold (fun acc jr ->
                    acc
                    |> Result.bind (fun r ->
                        r.AddJump(jr, competExists)
                        |> Result.mapError (fun e -> Competition.Error.Internal(string e))))

            // 5) Złóż Status według taga i wymaganych pól
            let! status =
                match snap.StatusTag with
                | Competition.StatusTag.NotStartedTag ->
                    match snap.GateState with
                    | Some gs ->
                        // Optional sanity
                        if not snap.DoneOrder.IsEmpty || not results.JumpResults.IsEmpty then
                            Error(
                                CompetitionRestoreHelpers.internalErr "NotStarted: expected no results and no progress"
                            )
                        else
                            Ok(Competition.Status.NotStarted gs)
                    | None -> Error(CompetitionRestoreHelpers.internalErr "NotStarted requires GateState")
                | Competition.StatusTag.RoundInProgressTag ->
                    match snap.GateState, snap.RoundIndex with
                    | Some gs, Some r -> Ok(Competition.Status.RoundInProgress(gs, r))
                    | _ ->
                        Error(CompetitionRestoreHelpers.internalErr "RoundInProgress requires GateState and RoundIndex")
                | Competition.StatusTag.SuspendedTag ->
                    match snap.GateState, snap.RoundIndex with
                    | Some gs, Some r -> Ok(Competition.Status.Suspended(gs, r))
                    | _ -> Error(CompetitionRestoreHelpers.internalErr "Suspended requires GateState and RoundIndex")
                | Competition.StatusTag.CancelledTag -> Ok Competition.Status.Cancelled
                | Competition.StatusTag.EndedTag ->
                    // Możemy (miękko) sprawdzić, czy runda wygląda na skończoną.
                    if not startlistAfterProgress.RoundIsFinished then
                        // Nie twardo – pozwalamy przywrócić (DB jest źródłem prawdy)
                        Ok Competition.Status.Ended
                    else
                        Ok Competition.Status.Ended

            // 6) Gotowe Competition
            return
                { Id = snap.Id
                  Settings = snap.Settings
                  Hill = snap.Hill
                  Status = status
                  Startlist = startlistAfterProgress
                  Results = results
                  Jumpers = snap.Jumpers }
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
                        raise (
                            Exception($"Next round startlist internal error: {nextStartlist |> Result.mapError string}")
                        )

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
