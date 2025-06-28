namespace App.Domain.Competitions

open System
open App.Domain.Competitions.RankedResults.Policy
open App.Domain.Shared.EventHelpers
open App.Domain

module Competition =
    type Id = Id of System.Guid

    type Settings = { Rules: Rules.Rules }

    type Phase =
        | NotStarted
        | Running of RoundIndex: RoundIndex
        | Break of NextRoundIndex: RoundIndex
        | Suspended of PreviousPhase: Phase
        | Cancelled
        | Ended

    type PhaseTag =
        | NotStartedTag
        | RunningTag
        | BreakTag
        | SuspendedTag
        | CancelledTag
        | EndedTag

    type Error =
        | InvalidPhase of Expected: PhaseTag list * Actual: PhaseTag
        | StartlistCompletionNotEmpty of Completed: Set<Participant.IndividualId>

    type Event =
        | CompetitionStarted of CompetitionId: Id * Timestamp: EventTimestamp
        | CompetitionRoundStarted of CompetitionId: Id * Timestamp: EventTimestamp * CurrentRoundIndex: RoundIndex
        | CompetitionRoundEnded of
            CompetitionId: Id *
            Timestamp: EventTimestamp *
            CurrentRoundIndex: RoundIndex *
            NextRoundIndex: RoundIndex option
        | CompetitionSuspended of CompetitionId: Id * Timestamp: EventTimestamp
        | CompetitionContinued of CompetitionId: Id * Timestamp: EventTimestamp
        | CompetitionCancelled of CompetitionId: Id * Timestamp: EventTimestamp
        | CompetitionEnded of CompetitionId: Id * Timestamp: EventTimestamp
        | CompetitionJumpResultRegistered of CompetitionId: Id * Timestamp: EventTimestamp * JumpResultId: Results.JumpResult.Id

open Competition

type Competition =
    { Id: Competition.Id
      HillId: Competitions.Hill.Id
      Phase: Competition.Phase
      Results: Results
      Startlist: Startlist
      Settings: Settings }

    static member TagOfPhase phase =
        match phase with
        | NotStarted -> NotStartedTag
        | Running _ -> RunningTag
        | Break _ -> BreakTag
        //| Suspended previousPhase -> SuspendedTag(Competition.TagOfPhase(previousPhase))
        | Suspended _ -> SuspendedTag
        | Cancelled -> CancelledTag
        | Ended -> EndedTag

    member this.InvalidPhaseError expected =
        InvalidPhase(expected, Competition.TagOfPhase(this.Phase))

    static member Create id resultsId (hillId: Hill.Id) (startlist: Startlist) rules =
        if not startlist.NoOneHasCompleted then
            Error(Error.StartlistCompletionNotEmpty(startlist.CompletedSet))
        else
            Ok
                { Id = id
                  HillId = hillId
                  Phase = NotStarted
                  Results = Results.Empty(resultsId) (TieBreakPolicy.ExAequo TieBreakPolicy.ExAequoPolicy.AddOneAfterExAequo) // TODO !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!1
                  Startlist = startlist
                  Settings = { Rules = rules } }

    /// TODO, Sprawdź przed dodaniem skoku: Czy w tej serii już skakał? Rzuć error.
    member this.RegisterJump (participantId: Participant.IndividualId) (jumpResult: Results.JumpResult) =
        match this.Startlist.Next with
        | Some nextIndividualParticipant ->
            match this.Phase with
            | NotStarted ->
                let roundIndex = RoundIndex 0u
                let totalPoints = 500m
                let competitionType = "team" // TODO
                match competitionType with
                | "team" ->
                    let teamId = Guid.NewGuid // TODO
                    let newResults = this.Results.RegisterJumpOfTeamParticipant(participantId teamId jumpResult 0 0)
        | None ->
            invalidOp "This should not be even raised, lmao"
        match this.Phase with
        | NotStarted ->
            let roundIndex = RoundIndex 0u
            let totalPoints = ParticipantResult.Points.tryCreate (500m) // TODO

            let newResults =
                this.Results.RegisterJumpResult participantId jumpResult totalPoints timestamp

            let state =
                { this with
                    Results = newResults
                    Phase = Phase.Running roundIndex }

            let startedCompetitionEvent = Event.CompetitionStarted(this.Id, timestamp)

            let jumpRegisteredEvent =
                Event.CompetitionJumpResultRegistered(this.Id, timestamp, jumpResult.Id)

            Ok(state, [ startedCompetitionEvent ])
        | Running currentRoundIndex ->
            if this.Results.ContainsJumpForRound currentRoundIndex participantId then
                Error 
            
            let totalPoints = ParticipantResult.Points.tryCreate (500m) // TODO

            let newResults =
                this.Results.RegisterJumpResult participantId jumpResult totalPoints timestamp

            let state =
                { this with
                    Results = newResults
                    Phase = Phase.Running currentRoundIndex }

            let startedCompetitionEvent = Event.CompetitionStarted(this.Id, timestamp)

            let jumpRegisteredEvent =
                Event.CompetitionJumpResultRegistered(this.Id, timestamp, jumpResult.Id)

            Ok(state, [ startedCompetitionEvent ])
        | Break nextRoundIndex ->
            let totalPoints = ParticipantResult.Points.tryCreate (500m) // TODO

            let newResults =
                this.Results.RegisterJumpResult participantId jumpResult totalPoints timestamp

            let state =
                { this with
                    Results = newResults
                    Phase = Phase.Running nextRoundIndex }

            let startedCompetitionEvent = Event.CompetitionStarted(this.Id, timestamp)

            let jumpRegisteredEvent =
                Event.CompetitionJumpResultRegistered(this.Id, timestamp, jumpResult.Id)

            Ok(state, [ startedCompetitionEvent ])
        | _ -> Error(this.InvalidPhaseError([ PhaseTag.NotStartedTag; PhaseTag.BreakTag; PhaseTag.RunningTag ]))

    // member this.ModifyCurrentRoundResult participantId newResult

    member this.Disqualify participantId timestamp =
        // TODO:
        // Jeśli drużynówka, w zależności od policy zdyskwalifikuj całą drużynę albo jednego zawodnika
        // Jeśli indywidualnie, DSQ w aktualnej rundzie i niemożność startu później
        None

    member this.Cancel timestamp =
        match this.Phase with
        | Phase.Suspended _
        | Phase.NotStarted
        | Phase.Break _
        | Phase.Running _ ->
            let state = { this with Phase = Phase.Cancelled }
            let event = Event.CompetitionCancelled(this.Id, timestamp)
            Ok(state, [ event ])
        | _ -> Error(this.InvalidPhaseError([ PhaseTag.SuspendedTag; PhaseTag.NotStartedTag; PhaseTag.BreakTag; PhaseTag.RunningTag ]))

    member this.Suspend timestamp =
        match this.Phase with
        | Phase.NotStarted
        | Phase.Break _
        | Phase.Running _ ->
            let state =
                { this with
                    Phase = Phase.Suspended this.Phase }

            let event = Event.CompetitionSuspended(this.Id, timestamp)
            Ok(state, [ event ])
        | _ ->
            Error(
                InvalidPhase(
                    [ PhaseTag.NotStartedTag; PhaseTag.BreakTag; PhaseTag.RunningTag ],
                    Competition.TagOfPhase(this.Phase)
                )
            )

    member this.Continue timestamp =
        match this.Phase with
        | Phase.Suspended previousState ->
            let state = { this with Phase = previousState }

            let event = Event.CompetitionContinued(this.Id, timestamp)
            Ok(state, [ event ])
        | _ -> Error(InvalidPhase([ PhaseTag.SuspendedTag ], Competition.TagOfPhase(this.Phase)))
