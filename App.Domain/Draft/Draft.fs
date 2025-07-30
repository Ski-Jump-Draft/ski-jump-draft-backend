namespace App.Domain.Draft

open App.Domain.Draft.Event
open App.Domain.Draft.Picks
open App.Domain.Draft.Order
open App.Domain.Draft.Settings
open App.Domain.Shared.AggregateVersion

type PhaseTag =
    | NotStartedTag
    | RunningTag
    | DoneTag

type Phase =
    | NotStarted
    | Running of CurrentIndex: int * ParticipantOrder: Participant.Id list * Picks: Picks
    | Done of Picks

type Error =
    | InvalidPhase of expected: PhaseTag list * actual: PhaseTag
    | ParticipantNotAllowedToPick of Participant.Id
    | SubjectNotAvailable of Subject.Id
    | PickingLimitReached
    | SubjectAlreadyPicked

type Draft =
    private
        { Id: Id.Id
          Version: AggregateVersion
          Settings: Settings.Settings
          Participants: Participant.Participant list
          Subjects: Subject.Subject list
          Seed: uint64
          Phase: Phase }

    member this.Phase_ = this.Phase
    member this.Participants_ = this.Participants
    member this.Settings_ = this.Settings
    member this.Version_: AggregateVersion = this.Version
    member this.Id_ = this.Id

    static member TagOfPhase =
        function
        | NotStarted -> NotStartedTag
        | Running _ -> RunningTag
        | Done _ -> DoneTag

    static member Create
        id
        version
        settings
        participants
        subjects
        seed
        : Result<Draft * DraftEventPayload list, Error> =
        let state =
            { Id = id
              Version = version
              Settings = settings
              Participants = participants
              Subjects = subjects
              Seed = seed
              Phase = NotStarted }

        let subjectDtos =
            subjects
            |> List.map (fun s ->
                match s.Identity with
                | Subject.Identity.Jumper j ->
                    { Id = s.Id
                      Identity =
                        DraftSubjectIdentityDto.Jumper
                            { Name = j.Name
                              Surname = j.Surname
                              CountryCode = j.CountryCode } }
                | Subject.Identity.Team t ->
                    { Id = s.Id
                      Identity =
                        DraftSubjectIdentityDto.Team
                            { Name = t.Name
                              CountryCode = t.CountryCode } })

        let event: DraftCreatedV1 =
            { DraftId = id
              Settings =
                { Order = settings.Order
                  MaxJumpersPerPlayer = settings.MaxJumpersPerPlayer
                  PickTimeout = settings.PickTimeout
                  UniqueJumpers = settings.UniqueJumpers }
              Participants = participants |> List.map (fun p -> { DraftParticipantDto.Id = p.Id })
              Subjects = subjectDtos
              Seed = seed }

        Ok(state, [ DraftEventPayload.DraftCreatedV1 event ])

    member this.Start() =
        match this.Phase with
        | NotStarted ->
            let strategy = OrderStrategyFactory.create this.Settings.Order
            let participantIds = this.Participants |> List.map _.Id

            let initialOrder = strategy.ComputeInitialOrder(participantIds, this.Seed)

            let newState =
                { this with
                    Phase = Running(0, initialOrder, Picks.Empty initialOrder) }

            let payload: DraftStartedV1 = { DraftId = this.Id }

            Ok(newState, [ DraftEventPayload.DraftStartedV1 payload ])

        | _ -> Error(InvalidPhase([ NotStartedTag ], Draft.TagOfPhase this.Phase))

    member this.Pick(participantId: Participant.Id, subjectId: Subject.Id) =
        match this.Phase with
        | Running(currentIdx, order, picks) ->

            if not (this.Subjects |> List.exists (fun s -> s.Id = subjectId)) then
                Error(SubjectNotAvailable subjectId)
            elif order.[currentIdx] <> participantId then
                Error(ParticipantNotAllowedToPick participantId)
            elif picks.PicksNumberOf participantId >= int this.Settings.MaxJumpersPerPlayer then
                Error PickingLimitReached
            elif this.Settings.UniqueJumpers && picks.ContainsSubject subjectId then
                Error SubjectAlreadyPicked
            else
                let updatedPicks = picks.AddPick(participantId, subjectId)
                let totalAllowed = int this.Settings.MaxJumpersPerPlayer * List.length order
                let completedRounds = updatedPicks.Total() / List.length order

                let pickPayload: DraftSubjectPickedV2 =
                    { DraftId = this.Id
                      ParticipantId = participantId
                      SubjectId = subjectId
                      PickIndex = uint (updatedPicks.PicksNumberOf participantId - 1) }

                let pickEvent = DraftEventPayload.DraftSubjectPickedV2 pickPayload

                let endEvents =
                    if updatedPicks.Total() = totalAllowed then
                        [ DraftEventPayload.DraftEndedV1 { DraftId = this.Id } ]
                    else
                        []

                let strategy = OrderStrategyFactory.create this.Settings.Order

                let (nextOrder, nextIdx) =
                    strategy.ComputeNextOrder(order, currentIdx, completedRounds, this.Seed)

                let newPhase =
                    if updatedPicks.Total() = totalAllowed then
                        Done updatedPicks
                    else
                        Running(nextIdx, nextOrder, updatedPicks)

                let newState = { this with Phase = newPhase }
                Ok(newState, pickEvent :: endEvents)

        | _ -> Error(InvalidPhase([ RunningTag ], Draft.TagOfPhase this.Phase))
