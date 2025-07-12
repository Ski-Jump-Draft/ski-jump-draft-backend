namespace App.Domain.Competition

open System
open App.Domain.Competition.Phase
open App.Domain

module Competition =
    // type Settings = { Rules: Rules.Rules }

    type Error = InvalidPhase of Expected: PhaseTag list * Actual: PhaseTag

open Competition

// type Competition =
//     { Id: Competition.Id
//       HillId: Competition.Hill.Id
//       Phase: Phase.Phase
//       ResultsId: Results.Id
//       Startlist: Startlist
//       Settings: Settings }

module internal Internal =
    let tag =
        function
        | NotStarted -> NotStartedTag
        | Running _ -> RunningTag
        | Break _ -> BreakTag
        | Suspended _ -> SuspendedTag
        | Cancelled -> CancelledTag
        | Ended -> EndedTag

    let expect phases actual = Error(InvalidPhase(phases, tag actual))

    let ok state (events: Event.CompetitionEventPayload list) = Ok(state, events)

open Internal

type Competition =
    { Id: Id.Id
      Phase: Phase.Phase
      StartlistId: Competition.Startlist.Id
      //HillId: Competition.Hill.Id
      ResultsId: ResultsModule.Id
    //Startlist: Startlist
    //Settings: Settings
    }

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

    static member Create id startlistId resultsId =
        let state =
            { Id = id
              Phase = NotStarted
              StartlistId = startlistId
              ResultsId = resultsId }

        let event =
            Event.CompetitionCreatedV1
                { CompetitionId = id
                  StartlistId = startlistId
                  ResultsId = resultsId }

        Ok(state, [ event ])


    // static member Create id resultsId (hillId: Hill.Id) (startlist: Startlist) rules =
    //     if not startlist.NoOneHasCompleted then
    //         Error(Error.StartlistCompletionNotEmpty(startlist.CompletedSet))
    //     else
    //         Ok
    //             { Id = id
    //               HillId = hillId
    //               Phase = NotStarted
    //               Results = Results.Empty(resultsId) (TieBreakPolicy.ExAequo TieBreakPolicy.ExAequoPolicy.AddOneAfterExAequo) // TODO !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!1
    //               Startlist = startlist
    //               Settings = { Rules = rules } }
    //
    // /// TODO, Sprawdź przed dodaniem skoku: Czy w tej serii już skakał? Rzuć error.
    // member this.RegisterJump (participantId: Participant.IndividualId) (jumpResult: Results.JumpResult) =
    //     match this.Startlist.Next with
    //     | Some nextIndividualParticipant ->
    //         match this.Phase with
    //         | NotStarted ->
    //             let roundIndex = RoundIndex 0u
    //             let totalPoints = 500m
    //             let competitionType = "team" // TODO
    //             match competitionType with
    //             | "team" ->
    //                 let teamId = Guid.NewGuid // TODO
    //                 let newResults = this.Results.RegisterJumpOfTeamParticipant(participantId teamId jumpResult 0 0)
    //     | None ->
    //         invalidOp "This should not be even raised, lmao"
    //     match this.Phase with
    //     | NotStarted ->
    //         let roundIndex = RoundIndex 0u
    //         let totalPoints = ParticipantResult.Points.tryCreate (500m) // TODO
    //
    //         let newResults =
    //             this.Results.RegisterJumpResult participantId jumpResult totalPoints timestamp
    //
    //         let state =
    //             { this with
    //                 Results = newResults
    //                 Phase = Phase.Running roundIndex }
    //
    //         let startedCompetitionEvent = Event.CompetitionStarted(this.Id, timestamp)
    //
    //         let jumpRegisteredEvent =
    //             Event.CompetitionJumpResultRegistered(this.Id, timestamp, jumpResult.Id)
    //
    //         Ok(state, [ startedCompetitionEvent ])
    //     | Running currentRoundIndex ->
    //         if this.Results.ContainsJumpForRound currentRoundIndex participantId then
    //             Error
    //
    //         let totalPoints = ParticipantResult.Points.tryCreate (500m) // TODO
    //
    //         let newResults =
    //             this.Results.RegisterJumpResult participantId jumpResult totalPoints timestamp
    //
    //         let state =
    //             { this with
    //                 Results = newResults
    //                 Phase = Phase.Running currentRoundIndex }
    //
    //         let startedCompetitionEvent = Event.CompetitionStarted(this.Id, timestamp)
    //
    //         let jumpRegisteredEvent =
    //             Event.CompetitionJumpResultRegistered(this.Id, timestamp, jumpResult.Id)
    //
    //         Ok(state, [ startedCompetitionEvent ])
    //     | Break nextRoundIndex ->
    //         let totalPoints = ParticipantResult.Points.tryCreate (500m) // TODO
    //
    //         let newResults =
    //             this.Results.RegisterJumpResult participantId jumpResult totalPoints timestamp
    //
    //         let state =
    //             { this with
    //                 Results = newResults
    //                 Phase = Phase.Running nextRoundIndex }
    //
    //         let startedCompetitionEvent = Event.CompetitionStarted(this.Id, timestamp)
    //
    //         let jumpRegisteredEvent =
    //             Event.CompetitionJumpResultRegistered(this.Id, timestamp, jumpResult.Id)
    //
    //         Ok(state, [ startedCompetitionEvent ])
    //     | _ -> Error(this.InvalidPhaseError([ PhaseTag.NotStartedTag; PhaseTag.BreakTag; PhaseTag.RunningTag ]))
    //
    // // member this.ModifyCurrentRoundResult participantId newResult
    //
    // member this.Disqualify participantId timestamp =
    //     // TODO:
    //     // Jeśli drużynówka, w zależności od policy zdyskwalifikuj całą drużynę albo jednego zawodnika
    //     // Jeśli indywidualnie, DSQ w aktualnej rundzie i niemożność startu później
    //     None

    member this.StartRound() =
        match this.Phase with
        | NotStarted ->
            let roundIndex = RoundIndex 0u
            let state = { this with Phase = Running roundIndex }

            let events =
                [ Event.CompetitionRoundStartedV1
                      { CompetitionId = this.Id
                        RoundIndex = Convert.ToInt32 roundIndex } ]

            ok state events

        | Break nextRound ->
            let state = { this with Phase = Running nextRound }

            let events =
                [ Event.CompetitionRoundStartedV1
                      { CompetitionId = this.Id
                        RoundIndex = Convert.ToInt32 nextRound } ]

            ok state events

        | phase -> expect [ NotStartedTag; BreakTag ] phase


    member this.EndRound(endCompetition: bool) =
        match this.Phase with
        | Running roundIdx ->
            let (RoundIndex roundIdxUint) = roundIdx
            let nextRoundInt = Convert.ToInt32(roundIdxUint + 1u)
            let nextRoundUint = uint (nextRoundInt)

            let roundEnded =
                Event.CompetitionRoundEndedV1
                    { CompetitionId = this.Id
                      RoundIndex = Convert.ToInt32 roundIdx
                      NextRoundIndex = if endCompetition then Some nextRoundInt else None }

            if endCompetition then
                let state = { this with Phase = Ended }
                let events = [ roundEnded; Event.CompetitionEndedV1 { CompetitionId = this.Id } ]
                ok state events
            else
                let state =
                    { this with
                        Phase = Break(RoundIndex nextRoundUint) }

                let events = [ roundEnded ]
                ok state events

        | phase -> expect [ RunningTag ] phase

    member this.Cancel() =
        match this.Phase with
        | NotStarted
        | Break _
        | Running _
        | Suspended _ ->
            let state = { this with Phase = Cancelled }
            let events = [ Event.CompetitionCancelledV1 { CompetitionId = this.Id } ]
            Internal.ok state events
        | phase -> expect [ NotStartedTag; BreakTag; RunningTag; SuspendedTag ] phase


    member this.Suspend() =
        match this.Phase with
        | NotStarted
        | Break _
        | Running _ ->
            let state =
                { this with
                    Phase = Suspended this.Phase }

            let events = [ Event.CompetitionSuspendedV1 { CompetitionId = this.Id } ]
            ok state events
        | phase -> expect [ NotStartedTag; BreakTag; RunningTag ] phase


    member this.Continue() =
        match this.Phase with
        | Suspended previous ->
            let state = { this with Phase = previous }
            let events = [ Event.CompetitionContinuedV1 { CompetitionId = this.Id } ]
            ok state events
        | phase -> expect [ SuspendedTag ] phase
