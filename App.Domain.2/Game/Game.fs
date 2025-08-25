namespace App.Domain._2.Game

open App.Domain._2

type GameId = GameId of System.Guid

type StatusTag =
    | PreDraftTag
    | DraftTag
    | MainCompetitionTag
    | EndedTag
    | BreakTag

type Status =
    | PreDraft of Competition.Competition
    | Draft of Draft
    | MainCompetition of Competition.Competition
    | Ended of Ranking
    | Break of Next: StatusTag
    
type GameError =
    | DraftError of Error: DraftError
    | InvalidPhase

type Game = {
    Id: GameId
    Settings: Settings
    Status: Status
    Players: Players
    Jumpers: Jumpers
    Ranking: Ranking
} with
    override this.Equals(obj) =
        match obj with
        | :? Game as other -> this.Id = other.Id
        | _ -> false

    override this.GetHashCode() =
        hash this.Id
    // --- PRE DRAFT --- //
    member this.StartPreDraft =
        invalidOp "Not implemented" // TODO
        
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
        
    // --- MAIN COMPETITION --- //
    member this.StartMainCompetition =
        invalidOp "Not implemented" // TODO
        
    // --- COMPETITION SHARED --- //
    member this.AddJumpInCompetition =
        invalidOp "Not implemented" // TODO
    
    member this.AdjustGateByJury =
        invalidOp "Not implemented" // TODO
    
    // --- ENDING --- //
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