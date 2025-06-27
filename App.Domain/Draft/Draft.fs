namespace App.Domain.Draft

open App.Domain.Draft
open App.Domain.Draft.Event
open App.Domain.Draft.Picks

type Phase =
    | NotStarted
    | Running of Turn: Participant.Id * Picks.Picks
    | Done of Picks.Picks

type PhaseTag =
    | NotStartedTag
    | RunningTag
    | DoneTag

type Error =
    | InvalidPhase of Expected: PhaseTag list * Actual: PhaseTag
    | PickingLimitReached
    | JumperTaken
    | ParticipantNotAllowedToPick of ParticipantId: Participant.Id


type Draft =
    { Id: Id.Id
      Settings: Settings.Settings
      Participants: Participant.Id list
      Phase: Phase}

    static member TagOfPhase phase =
        match phase with
        | NotStarted -> NotStartedTag
        | Running _ -> RunningTag
        | Done _ -> DoneTag

    static member Create id (settings: Settings.Settings) (participants: Participant.Id list) : Draft =
        { Id = id
          Settings = settings
          Participants = participants
          Phase = Phase.NotStarted}

    member this.Start startedDraftEventId timestamp corr caus =
        match this.Phase with
        | Phase.NotStarted ->
            let first = List.head this.Participants
            let empty = Picks.Empty this.Participants

            let state =
                { this with
                    Phase = Phase.Running(first, empty) }

            let payload = { DraftId = this.Id; Settings = this.Settings; Participants = this.Participants; Seed = ??? TODO }: DraftStartedV1
            Ok(state, [ DraftEventPayload.DraftStartedV1 payload ])
        | _ -> Error(InvalidPhase([ NotStartedTag ], Draft.TagOfPhase(this.Phase)))

    member this.Pick(participantId: Participant.Id, subjectId: Subject.Id) =
        let settings = this.Settings

        match this.Phase with
        | Phase.Running(currentParticipantId, picks) ->
            if currentParticipantId <> participantId then
                Error (ParticipantNotAllowedToPick currentParticipantId)
            elif picks.PicksNumberOf currentParticipantId >= int settings.MaxJumpersPerPlayer then
                Error PickingLimitReached
            elif settings.UniqueJumpers && picks.ContainsSubject(subjectId) then
                Error JumperTaken
            else
                let picks = picks.AddPick(currentParticipantId, subjectId)

                let finishedPicks =
                    picks.Total() = int settings.MaxJumpersPerPlayer * List.length this.Participants

                let picksCount = picks.PicksNumberOf currentParticipantId

                let pickedPayload =
                    { DraftId = this.Id
                      ParticipantId = currentParticipantId
                      SubjectId = subjectId
                      PickIndex = uint (picksCount - 1) }
                    : Event.DraftSubjectPickedV2

                let endedPayload = { DraftId = this.Id }: Event.DraftEndedV1

                let newProgress =
                    if finishedPicks then
                        Phase.Done picks
                    else
                        let next =
                            picks.NextParticipant(settings.Order, this.Participants, currentParticipantId, this.Random)

                        Phase.Running(next, picks)

                let events =
                    if finishedPicks then
                        [ DraftEventPayload.DraftSubjectPickedV2 pickedPayload
                          DraftEventPayload.DraftEndedV1 endedPayload ]
                    else
                        [ DraftEventPayload.DraftSubjectPickedV2 pickedPayload ]

                ({ this with Phase = newProgress }, events) |> Ok

        | Phase.NotStarted
        | Phase.Done _ -> Error(InvalidPhase([ RunningTag ], Draft.TagOfPhase(this.Phase)))
