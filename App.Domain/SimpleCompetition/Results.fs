namespace App.Domain.SimpleCompetition

module JumpResult =
    type Id = Id of System.Guid
    type JudgePoints = private JudgePoints of double

    module JudgePoints =
        type Error = BelowZero

        let tryCreate (v: double) =
            if v >= 0 then Ok(JudgePoints v) else Error(Error.BelowZero)
        let value (JudgePoints v) =v 

    type GatePoints = GatePoints of double

    module GatePointsModule =
        let value (GatePoints v) = v

    type WindPoints = WindPoints of double

    module WindPointsModule =
        let value (WindPoints v) = v

    type TotalPoints = TotalPoints of double

    module TotalPointsModule =
        let value (TotalPoints v) = v

open JumpResult

type JumpResult =
    { Id: JumpResult.Id
      Jump: Jump
      CompetitorId: Competitor.Id
      TeamId: Team.Id option
      RoundIndex: RoundIndex
      GroupIndex: GroupIndex option
      JudgePoints: JudgePoints option
      GatePoints: GatePoints option
      WindPoints: WindPoints option
      TotalPoints: TotalPoints }

module Results =
    type Error =
        | CompetitorAlreadyHasResultInRound of Competitor.Id * RoundIndex: RoundIndex
        | CompetitorNotFound of Competitor.Id * RoundIndex: RoundIndex
        | JumpAlreadyExists of Jump.Id

type Results =
    private
        { JumpResults: JumpResult list }

    static member Empty = { JumpResults = List.empty }

    member this.AddJump(jumpResult: JumpResult, competExists: Competitor.Id -> bool) : Result<Results, Results.Error> =
        let jump = jumpResult.Jump

        if not (competExists jump.CompetitorId) then
            Error(Results.Error.CompetitorNotFound(jump.CompetitorId, jumpResult.RoundIndex))
        elif
            this.JumpResults
            |> List.exists (fun jumpResult ->
                jumpResult.Jump.CompetitorId = jump.CompetitorId
                && jumpResult.RoundIndex = jumpResult.RoundIndex)
        then
            Error(Results.Error.CompetitorAlreadyHasResultInRound(jump.CompetitorId, jumpResult.RoundIndex))
        else
            Ok
                { this with
                    JumpResults = jumpResult :: this.JumpResults }

    member this.TotalPointsOf(competitorId: Competitor.Id) : TotalPoints option =
        this.JumpResults
        |> List.filter (fun jumpResult -> jumpResult.Jump.CompetitorId = competitorId)
        |> List.sumBy (fun jumpResult -> let (TotalPoints p) = jumpResult.TotalPoints in p)
        |> function
            | 0.0 -> Option.None
            | s -> Some(TotalPoints s)

    member this.FinalClassification() =
        // agreguj punkty
        let totals =
            this.JumpResults
            |> List.groupBy _.Jump.CompetitorId
            |> List.map (fun (cid, js) ->
                let pts = js |> List.sumBy (fun j -> let (TotalPoints p) = j.TotalPoints in p)
                cid, pts)
            |> List.sortByDescending snd

        // nadaj miejsca z ex-aequo
        let mutable displayedRank = 0
        let mutable prevPts = nan

        totals
        |> List.mapi (fun i (cid, pts) ->
            let rank =
                if pts <> prevPts then
                    displayedRank <- i + 1

                prevPts <- pts
                displayedRank

            cid, TotalPoints pts, rank)

    member this.TeamTotals(teamOf: Map<Competitor.Id, Team.Id>) =
        this.JumpResults
        |> List.choose (fun jumpResult ->
            teamOf
            |> Map.tryFind jumpResult.Jump.CompetitorId
            |> Option.map (fun tid -> tid, jumpResult.TotalPoints))
        |> List.groupBy fst
        |> List.map (fun (tid, lst) ->
            let total = lst |> List.sumBy (fun (_, TotalPoints p) -> p) |> TotalPoints
            tid, total)
