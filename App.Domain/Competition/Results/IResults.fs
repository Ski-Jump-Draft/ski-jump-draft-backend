module App.Domain.Competition.ResultsModule2

open App.Domain.Competition.Phase
open App.Domain.Competition.Results.ResultObjects



type IResults =
    abstract member Update: Result: ParticipantResult * RoundIndex: RoundIndex -> unit
    abstract member GetJumpResultForRound: IndividualId: IndividualParticipant.Id * RoundIndex: RoundIndex -> JumpResult

    /// Returns results state at the end of RoundIndex round
    abstract member GetStateAtRoundEnd: RoundIndex: RoundIndex -> IResults

    abstract member GetStateAfterJump: JumpNumber: uint * RoundIndex: RoundIndex -> IResults

    /// Returns current IndividualResult, unless RoundIndex is specified
    abstract member GetIndividualResult:
        IndividualId: IndividualParticipant.Id * RoundIndex: RoundIndex option -> IndividualResult

    /// Returns current TeamResult, unless RoundIndex is specified
    abstract member GetTeamResult: TeamId: Team.Id * RoundIndex: RoundIndex option -> TeamResult
