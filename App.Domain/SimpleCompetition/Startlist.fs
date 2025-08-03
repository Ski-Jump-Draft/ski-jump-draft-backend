namespace App.Domain.SimpleCompetition

module Startlist =
    /// Number on a startlist (something like a BIB) starting from 1
    type Order = private Order of int

    module Order =
        type Error = | BelowOne

        let tryCreate (v: int) =
            if v < 1 then Error(Error.BelowOne) else Ok(Order v)

    type CompetitorEntry =
        { CompetitorId: Competitor.Id
          TeamId: Team.Id option
          Order: Order }

    type Error =
        | EmptyStartlist
        | DuplicatedJumper of JumperEntries: CompetitorEntry list
        | JumperNotNext of Competitor.Id
        | JumperAlreadyDone of Competitor.Id
        | JumperNotFound of Competitor.Id
        | NoJumpersRemaining

type Startlist =
    private
        { Remaining: Startlist.CompetitorEntry list
          Done: Startlist.CompetitorEntry list }
        
    member this.Remaining_ = this.Remaining
    member this.Done_ = this.Done
    
    static member Create(entries: Startlist.CompetitorEntry list) =
        let duplicates =
            entries
            |> List.groupBy _.CompetitorId
            |> List.choose (fun (id, xs) -> if List.length xs > 1 then Some xs else Option.None)
            |> List.concat

        if duplicates <> [] then
            Error(Startlist.Error.DuplicatedJumper duplicates)
        else
            Ok { Remaining = entries; Done = [] }


    member this.NextJumper() = this.Remaining |> List.tryHead

    member this.MarkJumpDone(competitorId: Competitor.Id) =
        match this.Remaining with
        | [] -> Error(Startlist.Error.NoJumpersRemaining)
        | competitorEntry :: competitorEntries when competitorEntry.CompetitorId = competitorId ->
            Ok(
                { Remaining = competitorEntries
                  Done = competitorEntry :: this.Done }
            )
        | _ -> Error(Startlist.Error.JumperNotNext competitorId)

    member this.RoundIsFinished = this.Remaining.IsEmpty

    member this.RemainingOrder = this.Remaining |> List.map (fun e -> e.CompetitorId)

    member this.FullOrder =
        (this.Done |> List.rev) @ this.Remaining |> List.map (fun e -> e.CompetitorId)
