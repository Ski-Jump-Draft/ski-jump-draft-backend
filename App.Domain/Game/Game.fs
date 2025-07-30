namespace App.Domain.Game

open System
open App.Domain
open App.Domain.Game.Event
open App.Domain.Game.Participant
open App.Domain.Game.Ranking
open App.Domain.Game.Settings
open App.Domain.Shared.AggregateVersion

// TODO: Ustawienia matchmakingu i pokoju gry. Game.Rules/Game.Settings
// TODO: Może dynamiczny globalny limit graczy na pokój?
// TODO: Pre-draft np. kwalifikacje

module Game =
    [<Struct>]
    type Date = Date of DateTimeOffset

    module Date =
        type Error = CannotBeInFuture of DateTimeOffset

        let create (v: DateTimeOffset, clock: Time.IClock) =
            if v > clock.Now then
                Error(CannotBeInFuture v)
            else
                Ok(Date v)

    type Phase =
        | PreDraft of PreDraft.Id.Id
        | Draft of Draft.Id.Id
        | Competition of Game.Competition.Id
        | Ended of GameRanking
        | Break of Next: PhaseTag

    and PhaseTag =
        | PreDraftTag
        | DraftTag
        | CompetitionTag
        | EndedTag
        | BreakTag of Next: PhaseTag

    type Error =
        | InvalidPhase of Expected: PhaseTag list * Actual: PhaseTag
        // | GameRoomFull
        | TooManyParticipants of Count: int * Max: int
        | ParticipantNotInGame of Participant

open Game

type Game =
    private
        { Id: Id.Id
          Version: AggregateVersion
          Phase: Phase
          Settings: Settings.Settings
          Participants: Participants }

    member this.Phase_ = this.Phase
    member this.Participants_ = this.Participants
    member this.Settings_ = this.Settings
    member this.Version_: AggregateVersion = this.Version
    member this.Id_ = this.Id

    static member TagOfPhase phase =
        match phase with
        | PreDraft _ -> PreDraftTag
        | Draft _ -> DraftTag
        | Competition _ -> CompetitionTag
        | Ended _ -> EndedTag
        | Break next -> BreakTag next

    static member Create
        id
        version
        (participantsList: Participant list)
        (settings: Settings)
        : Result<Game * GameEventPayload list, Error> =
        let participants = Participants.from participantsList

        let participantsCount = Participants.count participants
        let participantLimit = ParticipantLimit.value settings.ParticipantLimit

        if participantsCount > participantLimit then
            Error(Error.TooManyParticipants(int (participantsCount), int (participantLimit)))
        else
            let state =
                { Id = id
                  Version = version
                  Phase = Break PreDraftTag
                  Settings = settings
                  Participants = participants }

            let event: Event.GameCreatedV1 =
                { GameId = id
                  Settings = settings
                  Participants = participants }


            Ok(state, [ GameEventPayload.GameCreatedV1 event ])

    member this.Leave(participant: Participant) : Result<Game * GameEventPayload list, Error> =
        if not (this.ParticipantIsPresent(participant)) then
            Error(Game.Error.ParticipantNotInGame participant)
        else
            match this.Phase with
            | Break _
            | Draft _
            | Competition _
            | PreDraft _ ->
                let updatedParticipants = Participants.remove participant this.Participants

                let updatedGame =
                    { this with
                        Participants = updatedParticipants }

                let event: ParticipantLeftV1 =
                    { GameId = updatedGame.Id
                      Participant = participant }

                Ok(updatedGame, [ GameEventPayload.ParticipantLeftV1 event ])
            | _ ->
                Error(
                    InvalidPhase(
                        [ (PhaseTag.BreakTag PhaseTag.CompetitionTag)
                          (PhaseTag.BreakTag PhaseTag.DraftTag)
                          (PhaseTag.BreakTag PhaseTag.PreDraftTag)
                          PhaseTag.DraftTag
                          PhaseTag.CompetitionTag
                          PhaseTag.PreDraftTag ],
                        this.Phase |> Game.TagOfPhase
                    )
                )


    // TODO: Wyjątki w CanJoin!!!!!!
    // TODO: jeśli gracz jest zbanowany, false
    // TODO: jeśli gra prywatna, podaj hasło
    // TODO: jeśli trzeba czekać na potwierdzenie, NIE WIEM CO

    member this.RoomIsFull =
        let currentParticipantsCount = Participants.count this.Participants

        let participantsLimit =
            Settings.ParticipantLimit.value this.Settings.ParticipantLimit

        currentParticipantsCount >= participantsLimit

    member this.ParticipantIsPresent(participant: Participant) =
        Participants.contains participant this.Participants

    member this.StartPreDraft(preDraftId) =
        match this.Phase with
        | Break PhaseTag.PreDraftTag ->
            let state =
                { this with
                    Phase = PreDraft preDraftId } // TODO

            let event: PreDraftPhaseStartedV1 =
                { GameId = this.Id
                  PreDraftId = preDraftId }

            Ok(state, [ GameEventPayload.PreDraftPhaseStartedV1 event ])
        | _ -> Error(InvalidPhase([ PhaseTag.BreakTag PhaseTag.PreDraftTag ], Game.TagOfPhase(this.Phase)))

    member this.EndPreDraft() =
        match this.Phase with
        | PreDraft preDraftId ->
            let state =
                { this with
                    Phase = Break PhaseTag.PreDraftTag }

            let event: PreDraftPhaseEndedV1 =
                { GameId = this.Id
                  PreDraftId = preDraftId }

            Ok(state, [ GameEventPayload.PreDraftPhaseEndedV1 event ])
        | _ -> Error(InvalidPhase([ PhaseTag.PreDraftTag ], Game.TagOfPhase(this.Phase)))

    member this.StartDraft draftId =
        match this.Phase with
        | Break(Next = PhaseTag.DraftTag) ->
            let state =
                { this with
                    Phase = Phase.Draft draftId }

            let event: DraftPhaseStartedV1 = { GameId = this.Id; DraftId = draftId }
            Ok(state, [ GameEventPayload.DraftPhaseStartedV1 event ])
        | _ -> Error(InvalidPhase([ BreakTag PhaseTag.DraftTag ], Game.TagOfPhase this.Phase))

    member this.EndDraft() =
        match this.Phase with
        | Draft draftId ->
            let state =
                { this with
                    Phase = Phase.Break PhaseTag.CompetitionTag }

            let event: DraftPhaseEndedV1 = { GameId = this.Id; DraftId = draftId }
            Ok(state, [ GameEventPayload.DraftPhaseEndedV1 event ])
        | _ -> Error(InvalidPhase([ DraftTag ], Game.TagOfPhase this.Phase))

    member this.StartCompetition competitionId =
        match this.Phase with
        | Break(Next = PhaseTag.CompetitionTag) ->
            let state =
                { this with
                    Phase = Phase.Competition competitionId }

            let event: CompetitionPhaseStartedV1 =
                { GameId = this.Id
                  CompetitionId = competitionId }

            Ok(state, [ GameEventPayload.CompetitionPhaseStartedV1 event ])

        | _ -> Error(InvalidPhase([ BreakTag PhaseTag.CompetitionTag ], Game.TagOfPhase this.Phase))

    member this.EndCompetition() =
        match this.Phase with
        | Competition competitionId ->
            let state =
                { this with
                    Phase = Phase.Break PhaseTag.EndedTag }

            let event: CompetitionPhaseEndedV1 =
                { GameId = this.Id
                  CompetitionId = competitionId }

            Ok(state, [ GameEventPayload.CompetitionPhaseEndedV1 event ])
        | _ -> Error(InvalidPhase([ CompetitionTag ], Game.TagOfPhase this.Phase))

    member this.EndGame endedGameResults =
        match this.Phase with
        | Break(Next = PhaseTag.EndedTag) ->
            let state =
                { this with
                    Phase = Phase.Ended endedGameResults }

            let event: GameEndedV1 =
                { GameId = this.Id
                  Ranking = endedGameResults }

            Ok(state, [ GameEventPayload.GameEndedV1 event ])
        | _ -> Error(InvalidPhase([ BreakTag PhaseTag.EndedTag ], Game.TagOfPhase this.Phase))
