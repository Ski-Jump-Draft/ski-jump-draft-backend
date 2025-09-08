namespace App.Domain.Competition

module Startlist =
    type Bib = private Bib of int

    module Bib =
        let create value =
            if value >= 1 then Some(Bib value) else None

        let value (Bib v) = v

    type Entry = { JumperId: JumperId; Bib: Bib }

    type Error =
        | EmptyStartlist
        | DuplicatedJumper of JumperId list
        | DuplicatedBib of int list
        | JumperNotNext of attempted: JumperId * nextShouldBe: Entry
        | JumperAlreadyDone of JumperId
        | JumperNotFound of JumperId
        | NoJumpersRemaining
        | UnknownCompetitor of JumperId
        | OrderLengthMismatch of provided: int * expected: int
        | Internal

type Startlist =
    private
        { Remaining: Startlist.Entry list
          Done: Startlist.Entry list
          BibOfMap: Map<JumperId, Startlist.Bib> }

    override this.ToString() =
        let showEntry (e: Startlist.Entry) =
            $"({e.JumperId}, Bib={Startlist.Bib.value e.Bib})"

        let rem = this.Remaining |> List.map showEntry |> String.concat "; "
        let done' = this.Done |> List.map showEntry |> String.concat "; "
        $"Remaining=[{rem}] | Done=[{done'}]"

    static member CreateLinear(jumperIds: JumperId list) : Result<Startlist, Startlist.Error> =
        if List.isEmpty jumperIds then
            Error Startlist.Error.EmptyStartlist
        else
            let duplicatedJumperIds =
                jumperIds
                |> List.groupBy id
                |> List.choose (fun (jid, xs) -> if xs.Length > 1 then Some jid else None)

            if not duplicatedJumperIds.IsEmpty then
                Error(Startlist.Error.DuplicatedJumper duplicatedJumperIds)
            else
                let entryResults =
                    jumperIds
                    |> List.mapi (fun index jumperId ->
                        match Startlist.Bib.create (index + 1) with
                        | Some bib -> Ok({ JumperId = jumperId; Bib = bib }: Startlist.Entry)
                        | None -> Error Startlist.Error.Internal)

                let rec collect acc =
                    function
                    | [] -> Ok(List.rev acc)
                    | Ok e :: rest -> collect (e :: acc) rest
                    | Error err :: _ -> Error err

                match collect [] entryResults with
                | Error err -> Error err
                | Ok entries ->
                    let bibLookup = entries |> List.map (fun e -> e.JumperId, e.Bib) |> Map.ofList

                    Ok
                        { Remaining = entries
                          Done = []
                          BibOfMap = bibLookup }

    static member CreateWithBibs(assigned: (JumperId * Startlist.Bib) list) : Result<Startlist, Startlist.Error> =
        if List.isEmpty assigned then
            Error Startlist.Error.EmptyStartlist
        else
            let duplicatedJumperIds =
                assigned
                |> List.groupBy fst
                |> List.choose (fun (jid, xs) -> if xs.Length > 1 then Some jid else None)

            if not duplicatedJumperIds.IsEmpty then
                Error(Startlist.Error.DuplicatedJumper duplicatedJumperIds)
            else
                let duplicatedBibValues =
                    assigned
                    |> List.map (fun (_, bib) -> Startlist.Bib.value bib)
                    |> List.groupBy id
                    |> List.choose (fun (bibValue, xs) -> if xs.Length > 1 then Some bibValue else None)

                if not duplicatedBibValues.IsEmpty then
                    Error(Startlist.Error.DuplicatedBib duplicatedBibValues)
                else
                    let entries =
                        assigned
                        |> List.map (fun (jumperId, bib) ->
                            { Startlist.Entry.JumperId = jumperId
                              Bib = bib }
                            : Startlist.Entry)
                        |> List.sortBy (fun e -> Startlist.Bib.value e.Bib)

                    let bibLookup = assigned |> Map.ofList

                    Ok
                        { Remaining = entries
                          Done = []
                          BibOfMap = bibLookup }

    member this.NextEntry: Startlist.Entry option = this.Remaining |> List.tryHead

    member this.MarkJumpDone(jumperId: JumperId) : Result<Startlist, Startlist.Error> =
        match this.Remaining with
        | [] -> Error Startlist.Error.NoJumpersRemaining
        | next :: tail when next.JumperId = jumperId ->
            Ok
                { this with
                    Remaining = tail
                    Done = next :: this.Done }
        | next :: _ -> Error(Startlist.Error.JumperNotNext(jumperId, next))

    member this.RoundIsFinished: bool = this.Remaining.IsEmpty

    member this.RemainingEntries: Startlist.Entry list = this.Remaining

    member this.RemainingJumperIds: JumperId list = this.Done |> List.map (_.JumperId)

    member this.DoneEntries: Startlist.Entry list = this.Done

    member this.FullEntries: Startlist.Entry list =
        (this.Done |> List.rev) @ this.Remaining

    member this.BibOf(jumperId: JumperId) : Startlist.Bib option = this.BibOfMap |> Map.tryFind jumperId

    static member WithOrder (previous: Startlist) (order: JumperId list) : Result<Startlist, Startlist.Error> =

        let knownJumperIds = previous.BibOfMap |> Map.toList |> List.map fst |> Set.ofList
        let providedJumperIds = order |> Set.ofList

        if order.Length <> knownJumperIds.Count then
            Error(Startlist.Error.OrderLengthMismatch(order.Length, knownJumperIds.Count))
        elif not (Set.isSubset providedJumperIds knownJumperIds) then
            let firstUnknown =
                order
                |> List.tryFind (fun jid -> not (Set.contains jid knownJumperIds))
                |> Option.get

            Error(Startlist.Error.UnknownCompetitor firstUnknown)
        else
            let entries =
                order
                |> List.map (fun jid ->
                    let bib =
                        previous.BibOfMap
                        |> Map.tryFind jid
                        |> Option.defaultWith (fun _ -> invalidOp "Missing BIB")

                    { Startlist.Entry.JumperId = jid
                      Bib = bib }
                    : Startlist.Entry)

            Ok
                { Remaining = entries
                  Done = []
                  BibOfMap = previous.BibOfMap }
