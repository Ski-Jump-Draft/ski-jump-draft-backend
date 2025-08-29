namespace App.Domain._2.Competition

type JumpResultId = JumpResultId of System.Guid

type TotalPoints = TotalPoints of double

module TotalPoints =
    let value (TotalPoints v) = v

module JumpResult =
    type JudgePoints = private JudgePoints of double

    module JudgePoints =
        type Error = BelowZero

        let tryCreate (v: double) =
            if v >= 0 then Ok(JudgePoints v) else Error(Error.BelowZero)

        let value (JudgePoints v) = v

    type GatePoints = GatePoints of double

    module GatePoints =
        let value (GatePoints v) = v

    type WindPoints = WindPoints of double

    module WindPoints =
        let value (WindPoints v) = v

module Classification =
    type Position = private Position of int

    module Position =
        let tryCreate (v: int) =
            if v < 1 then None else Some(Position v)
        let value (Position v) = v
        
    type JumperClassificationResult =
        { JumperId: JumperId
          Points: TotalPoints
          Position: Position }

open JumpResult

type JumpResult =
    { Id: JumpResultId
      JumperId: JumperId
      Jump: Jump
      RoundIndex: RoundIndex
      JudgePoints: JudgePoints option
      GatePoints: GatePoints option
      WindPoints: WindPoints option
      TotalPoints: TotalPoints }

module Results =
    type Error =
        | CompetitorAlreadyHasResultInRound of JumperId * RoundIndex: RoundIndex
        | CompetitorNotFound of JumperId * RoundIndex: RoundIndex
        | JumpAlreadyExists of Jump.Id

type Results =
    private
        { JumpResults: JumpResult list }

    static member Empty = { JumpResults = List.empty }

    member this.AddJump(jumpResult: JumpResult, competExists: JumperId -> bool) : Result<Results, Results.Error> =
        let jump = jumpResult.Jump

        if not (competExists jump.JumperId) then
            Error(Results.Error.CompetitorNotFound(jump.JumperId, jumpResult.RoundIndex))
        elif
            this.JumpResults
            |> List.exists (fun existing ->
                existing.Jump.JumperId = jump.JumperId
                && existing.RoundIndex = jumpResult.RoundIndex)
        then
            Error(Results.Error.CompetitorAlreadyHasResultInRound(jump.JumperId, jumpResult.RoundIndex))
        else
            Ok
                { this with
                    JumpResults = jumpResult :: this.JumpResults }


    member this.TotalPointsOf(competitorId: JumperId) : TotalPoints option =
        this.JumpResults
        |> List.filter (fun jumpResult -> jumpResult.Jump.JumperId = competitorId)
        |> List.sumBy (fun jumpResult -> let (TotalPoints p) = jumpResult.TotalPoints in p)
        |> function
            | 0.0 -> Option.None
            | s -> Some(TotalPoints s)

    member this.FinalClassification() =
        let totals =
            this.JumpResults
            |> List.groupBy _.Jump.JumperId
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

            { Classification.JumperClassificationResult.JumperId = cid
              Points = TotalPoints pts
              Position = (Classification.Position.tryCreate rank).Value }
            : Classification.JumperClassificationResult)

    member this.PositionOf(jumperId: JumperId) : Classification.Position option =
        this.FinalClassification()
        |> List.tryFind (fun competitorTotal -> competitorTotal.JumperId = jumperId)
        |> Option.map _.Position

    member this.TotalsSinceReset (resets: RoundIndex list) (upToRound: RoundIndex) =
        let (RoundIndex upTo) = upToRound

        let lastResetOpt =
            resets
            |> List.filter (fun (RoundIndex r) -> r < upTo)
            |> function
                | [] -> None
                | xs -> Some(List.max xs)

        this.JumpResults
        |> List.filter (fun jr ->
            match lastResetOpt with
            | Some(RoundIndex reset) ->
                let (RoundIndex r) = jr.RoundIndex
                r > reset
            | None -> true)
        |> List.groupBy (fun jr -> jr.JumperId)
        |> Map.ofList
        |> Map.map (fun _ jumps -> jumps |> List.sumBy (fun j -> let (TotalPoints p) = j.TotalPoints in p))
