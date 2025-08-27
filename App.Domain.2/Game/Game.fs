namespace App.Domain._2.Game

open App.Domain._2

type GameId = GameId of System.Guid

type StatusTag =
    | PreDraftTag
    | DraftTag
    | MainCompetitionTag
    | EndedTag
    | BreakTag

type PreDraftCompetitionIndex = private PreDraftCompetitionIndex of int
module PreDraftCompetitionIndex =
    let create (v: int) =
        if v < 0 then
            None
        else
            Some(PreDraftCompetitionIndex v)
    let value (PreDraftCompetitionIndex v) = v
    let next (v: PreDraftCompetitionIndex) =
        let (PreDraftCompetitionIndex index) = v
        (create (index + 1)).Value

type PreDraftStatus =
    | Break of NextIndex: PreDraftCompetitionIndex
    | Running of Index: PreDraftCompetitionIndex * Competition: Competition.Competition

type Status =
    | PreDraft of PreDraftStatus
    | Draft of Draft
    | MainCompetition of Competition.Competition
    | Ended of Ranking
    | Break of Next: StatusTag
    
type GameError =
    | HillAlreadySet
    | HillRequired
    | PreDraftNotEndedYet
    | PreDraftCompetitionAlreadyEnded
    | DraftError of Error: DraftError
    | CompetitionError of Error: Competition.Competition.Error
    | InvalidPhase
    | Internal of InnerError: string

module private GameHelpers =
    let hasNextPreDraftCompetition (settings: Settings) (index: PreDraftCompetitionIndex) =
        let current = PreDraftCompetitionIndex.value index
        current + 1 < settings.PreDraftSettings.Competitions.Length
        
    let createCompetition settings hill competitionId competitionJumpers startingGate =
        Competition.Competition.Create(competitionId, settings, hill, competitionJumpers, startingGate)
        
    let updatePreDraftAfterCompetition settings index (competition: Competition.Competition) =
        match competition.GetStatusTag with
        | Competition.Competition.StatusTag.EndedTag ->
            if hasNextPreDraftCompetition settings index then
                let nextIndex = PreDraftCompetitionIndex.next index
                Status.PreDraft (PreDraftStatus.Break nextIndex)
            else
                Status.Break DraftTag
        | _ ->
            Status.PreDraft (PreDraftStatus.Running(index, competition))

[<CustomEquality; NoComparison>]
type Game = {
    Id: GameId
    Settings: Settings
    Status: Status
    Players: Players
    Jumpers: Jumpers
    Ranking: Ranking
    Hill: Competition.Hill option
} with
    override this.Equals(obj) =
        match obj with
        | :? Game as other -> this.Id = other.Id
        | _ -> false

    override this.GetHashCode() =
        hash this.Id
        
    /// Hill can be set once and must be before PreDraft. Sets a one hill for every competition in Game.
    member this.SetHill hill =
        match this.Hill with
        | None ->
            Ok({ this with Hill = hill })
        | Some _ ->
            Error(GameError.HillAlreadySet)
        
    /// Starts the PreDraft phase initializing it with the first competition
    member this.StartPreDraft
        (competitionId: Competition.CompetitionId)
        (competitionJumpers: Competition.Jumper list)
        (startingGate: Competition.Gate)
        : Result<Game, GameError> =

        let preDraftIdx = (PreDraftCompetitionIndex.create 0).Value

        match this.Hill, this.Status with
        | None, _ -> Error GameError.HillRequired
        | Some hill, Break PreDraftTag ->
            let settings = this.Settings.PreDraftSettings.Competitions[0]
            match GameHelpers.createCompetition settings hill competitionId competitionJumpers startingGate with
            | Ok competition ->
                Ok { this with Status = Status.PreDraft(PreDraftStatus.Running(preDraftIdx, competition)) }
            | Error e -> Error (GameError.CompetitionError e)
        | _ , _ -> Error GameError.InvalidPhase

    /// Advances to the next PreDraft competition, if should. If not, throws.
    member this.ContinuePreDraft
        (competitionId: Competition.CompetitionId)
        (competitionJumpers: Competition.Jumper list)
        (startingGate: Competition.Gate)
        : Result<Game, GameError> =

        match this.Hill, this.Status with
        | None, _ -> Error GameError.HillRequired
        | Some hill, PreDraft (PreDraftStatus.Break nextIdx) ->
            let i = PreDraftCompetitionIndex.value nextIdx
            let settings = this.Settings.PreDraftSettings.Competitions[i]
            match GameHelpers.createCompetition settings hill competitionId competitionJumpers startingGate with
            | Ok competition ->
                Ok { this with Status = Status.PreDraft(PreDraftStatus.Running(nextIdx, competition)) }
            | Error e -> Error (GameError.CompetitionError e)
        | _ , _ -> Error GameError.InvalidPhase

    // --- DRAFT --- //
    member this.StartDraft shuffleFn =
        let draftResult = Draft.Create this.Settings.DraftSettings (Players.toIdsList this.Players) (Jumpers.toIdsList this.Jumpers) shuffleFn
        match draftResult with
        | Error e ->Error(e)
        | Ok draft ->
            Ok({ this with Status = Status.Draft draft })
            
    member this.PickInDraft (playerId: PlayerId) (jumperId: JumperId) : Result<PickOutcome, GameError> =
        match this.Status with
        | Draft draft ->
            match draft.Pick playerId jumperId with
            | Error e -> Error (GameError.DraftError e)
            | Ok newDraft ->
                if newDraft.Ended then
                    let newStatus = Status.Break StatusTag.MainCompetitionTag
                    let newGame = { this with Status = newStatus }
                    Ok { Game = newGame; Picked = jumperId; PhaseChangedTo = Some newStatus }
                else
                    let newGame = { this with Status = Status.Draft newDraft }
                    Ok { Game = newGame; Picked = jumperId; PhaseChangedTo = None }
        | _ -> Error GameError.InvalidPhase
        
    member this.CurrentTurnInDraft =
        match this.Status with
        | Draft draft ->
            Ok draft.CurrentPlayer
        | _ -> Error(GameError.InvalidPhase)
        
    /// Starts the Competition phase of the Game
    member this.StartMainCompetition
        (competitionId: Competition.CompetitionId)
        (competitionJumpers: Competition.Jumper list)
        (startingGate: Competition.Gate) =
        
        match this.Hill, this.Status with
        | None, _ -> Error GameError.HillRequired
        | Some hill, Break MainCompetitionTag ->
            let settings = this.Settings.MainCompetitionSettings
            match GameHelpers.createCompetition settings hill competitionId competitionJumpers startingGate with
            | Ok competition ->
                Ok { this with Status = Status.MainCompetition competition }
            | Error e -> Error (GameError.CompetitionError e)
        | _ , _ -> Error GameError.InvalidPhase

    /// Adds a jump to the current competition (PreDraft or Main). Can change Game's Status to a Break.
    member this.AddJumpInCompetition
        (jumpResultId: Competition.JumpResultId)
        (jump: Competition.Jump) =
        match this.Hill, this.Status with
        | None, _ -> Error GameError.HillRequired

        | Some _, PreDraft (PreDraftStatus.Running(idx, comp)) ->
            match comp.AddJump(jumpResultId, jump) with
            | Error e -> Error (GameError.CompetitionError e)
            | Ok comp' ->
                let newStatus = GameHelpers.updatePreDraftAfterCompetition this.Settings idx comp'
                let phaseChanged =
                    match comp'.GetStatusTag with
                    | Competition.Competition.StatusTag.EndedTag -> Some newStatus
                    | _ -> None
                Ok { Game = { this with Status = newStatus }
                     Competition = comp'
                     PhaseChangedTo = phaseChanged }

        | Some _, MainCompetition comp ->
            match comp.AddJump(jumpResultId, jump) with
            | Error e -> Error (GameError.CompetitionError e)
            | Ok comp' ->
                let ended = comp'.GetStatusTag = Competition.Competition.StatusTag.EndedTag
                let newStatus =
                    if ended then Status.Break EndedTag
                    else Status.MainCompetition comp'
                Ok { Game = { this with Status = newStatus }
                     Competition = comp'
                     PhaseChangedTo = if ended then Some newStatus else None }

        | _ , _ -> Error GameError.InvalidPhase
    
    member this.AdjustGateByJury =
        invalidOp "Not implemented" // TODO
    
    /// Ends the game based on a Ranking passed from Application Layer and drawn from some factory
    member this.EndGame (ranking: Ranking) =
        match this.Status with
        | Break EndedTag ->
            Ok({ this with Status = Ended ranking })
        | _ -> Error GameError.InvalidPhase
        
and PickOutcome =
    {
        Game: Game
        Picked: JumperId
        PhaseChangedTo: Status option
    }
and AddJumpOutcome =
    {
        Game: Game
        Competition: Competition.Competition
        PhaseChangedTo: Status option
    }