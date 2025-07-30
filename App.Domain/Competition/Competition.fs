namespace App.Domain.Competition

open App.Domain.Competition.Engine
open App.Domain.Competition.Phase
open App.Domain
open App.Domain.Competition.ResultsModule

// TODO: Evolve dla Competition na podstawie eventów domenowych ze startlist i results (!!)

module Competition =
    type Error =
        | InvalidPhase of Expected: PhaseTag list * Actual: PhaseTag
        | EngineEventsConflict of UnallowedEvents: Engine.Event list * Message: string
        | CannotDetermineCompetitionPhase of EngineEvents: Engine.Event list

open Competition

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
      Engine: Engine.IEngine }

    static member TagOfPhase phase =
        match phase with
        | NotStarted -> NotStartedTag
        | Running _ -> RunningTag
        | Break _ -> BreakTag
        | Suspended _ -> SuspendedTag
        | Cancelled -> CancelledTag
        | Ended -> EndedTag

    member this.InvalidPhaseError expected =
        InvalidPhase(expected, Competition.TagOfPhase(this.Phase))

    static member Create id (engine: IEngine) startlistId resultsId =
        let startlist = engine.GenerateStartlist()

        let state =
            { Id = id
              Phase = NotStarted
              Engine = engine }

        let event =
            Event.CompetitionCreatedV1
                { CompetitionId = id
                  Startlist = { NextIndividualParticipants = startlist.NextIndividualParticipants } }

        Ok(state, [ event ])

    member this.GenerateResults: Results = this.Engine.GenerateResults()

    member this.GenerateStartlist: Startlist = this.Engine.GenerateStartlist()

    member this.RegisterJump
        (jump: Competition.Jump.Jump)
        : Result<Competition * Event.CompetitionEventPayload list, Error> =
        match this.Phase with
        | NotStarted
        | Running _
        | Break _ ->
            let (snapshot, engineEvents) = this.Engine.RegisterJump jump

            let competitionEvents =
                engineEvents
                |> List.fold
                    (fun acc e ->
                        let newEvent =
                            match e with
                            | RoundStarted index ->
                                Event.CompetitionEventPayload.CompetitionRoundStartedV1
                                    { CompetitionId = this.Id
                                      RoundIndex = index }
                            | RoundEnded index ->
                                Event.CompetitionEventPayload.CompetitionRoundEndedV1
                                    { CompetitionId = this.Id
                                      RoundIndex = index }
                            | JumpRegistered(participantResultId, jumpResultId) ->
                                let startlist = this.Engine.GenerateStartlist()
                                let results = this.Engine.GenerateResults()

                                Event.CompetitionEventPayload.CompetitionJumpResultRegisteredV1
                                    { CompetitionId = this.Id
                                      ParticipantResultId = participantResultId
                                      JumpResultId = jumpResultId
                                      Startlist = { NextIndividualParticipants = startlist.NextIndividualParticipants }
                                      Results = { ParticipantResults = results.ParticipantResults } }
                            | ParticipantDisqualified -> failwith "todo"
                            | CompetitionEnded ->
                                Event.CompetitionEventPayload.CompetitionEndedV1 { CompetitionId = this.Id }

                        newEvent :: acc)
                    []
                |> List.rev

            let jumpResultHasBeenRegistered =
                competitionEvents
                |> List.exists (function
                    | Event.CompetitionEventPayload.CompetitionJumpResultRegisteredV1 _ -> true
                    | _ -> false)

            let roundHasStarted =
                competitionEvents
                |> List.exists (function
                    | Event.CompetitionEventPayload.CompetitionRoundStartedV1 _ -> true
                    | _ -> false)

            let roundHasEnded =
                competitionEvents
                |> List.exists (function
                    | Event.CompetitionEventPayload.CompetitionRoundEndedV1 _ -> true
                    | _ -> false)

            let competitionHasEnded =
                competitionEvents
                |> List.exists (function
                    | Event.CompetitionEventPayload.CompetitionEndedV1 _ -> true
                    | _ -> false)

            let engineEventsHasDuplicates =
                List.length engineEvents <> Set.count (Set.ofList engineEvents)

            if engineEventsHasDuplicates then
                Error(Error.EngineEventsConflict(engineEvents, "Engine cannot return engineEvents with duplicates."))
            elif not (jumpResultHasBeenRegistered) then
                Error(Error.EngineEventsConflict(engineEvents, "Engine must return JumpRegistered event."))
            elif roundHasStarted && roundHasEnded then
                Error(Error.EngineEventsConflict(engineEvents, "Engine cannot start and end round simultanousely."))
            else
                let competitionPhase =
                    if roundHasStarted then
                        let roundIndex =
                            competitionEvents
                            |> List.pick (function
                                | Event.CompetitionEventPayload.CompetitionRoundStartedV1 ev -> Some ev.RoundIndex
                                | _ -> None)

                        Ok(Phase.Running(RoundIndex roundIndex))
                    elif roundHasEnded && not competitionHasEnded then
                        let nextRoundIndex =
                            competitionEvents
                            |> List.pick (function
                                | Event.CompetitionEventPayload.CompetitionRoundStartedV1 ev -> Some ev.RoundIndex
                                | _ -> None)

                        Ok(Phase.Break(RoundIndex nextRoundIndex))
                    elif roundHasEnded && competitionHasEnded then
                        Ok Phase.Ended
                    else
                        Error(CannotDetermineCompetitionPhase engineEvents)

                match competitionPhase with
                | Error error -> Error error
                | Ok competitionPhase ->
                    let state = { this with Phase = competitionPhase }
                    Ok(state, competitionEvents)

        | phase -> expect [ NotStartedTag; BreakTag; RunningTag ] phase

    // member this.StartRound() =
    //     match this.Phase with
    //     | NotStarted ->
    //         let roundIndex = RoundIndex 0u
    //         let state = { this with Phase = Running roundIndex }
    //
    //         let events =
    //             [ Event.CompetitionRoundStartedV1
    //                   { CompetitionId = this.Id
    //                     RoundIndex = Convert.ToInt32 roundIndex } ]
    //
    //         ok state events
    //
    //     | Break nextRound ->
    //         let state = { this with Phase = Running nextRound }
    //
    //         let events =
    //             [ Event.CompetitionRoundStartedV1
    //                   { CompetitionId = this.Id
    //                     RoundIndex = Convert.ToInt32 nextRound } ]
    //
    //         ok state events
    //
    //     | phase -> expect [ NotStartedTag; BreakTag ] phase
    //
    //
    // member this.EndRound(endCompetition: bool) =
    //     match this.Phase with
    //     | Running roundIdx ->
    //         let (RoundIndex roundIdxUint) = roundIdx
    //         let nextRoundInt = Convert.ToInt32(roundIdxUint + 1u)
    //         let nextRoundUint = uint (nextRoundInt)
    //
    //         let roundEnded =
    //             Event.CompetitionRoundEndedV1
    //                 { CompetitionId = this.Id
    //                   RoundIndex = Convert.ToInt32 roundIdx
    //                   NextRoundIndex = if endCompetition then Some nextRoundInt else None }
    //
    //         if endCompetition then
    //             let state = { this with Phase = Ended }
    //             let events = [ roundEnded; Event.CompetitionEndedV1 { CompetitionId = this.Id } ]
    //             ok state events
    //         else
    //             let state =
    //                 { this with
    //                     Phase = Break(RoundIndex nextRoundUint) }
    //
    //             let events = [ roundEnded ]
    //             ok state events
    //
    //     | phase -> expect [ RunningTag ] phase

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
