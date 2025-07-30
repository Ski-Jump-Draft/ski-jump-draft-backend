module App.Domain.Competition.Results.RankedResults

open App.Domain.Competition.Phase

type Position = private Position of int

module Position =
    type Error = ZeroOrNegative

    let tryCreate v =
        if v >= 1 then Ok(Position v) else Error ZeroOrNegative

    let value (Position v) = v

// type Participant =
//     { Id: ParticipantResult.Id
//       Position: Position }
//
//
// module Policy =
//     type ChooseOnePolicy =
//         | MoreRecentPoints
//         | FurtherLastDistance
//         | Alphabetical
//         | Random
//         | Custom of CustomStrategy.Ref
//
//     type ExAequoPolicy =
//         | AddOneAfterExAequo
//         | ContinueNormallyAfterExAequo
//
//     type TieBreakPolicy =
//         | ExAequo of ExAequoPolicy
//         | ChooseOne of ChooseOnePolicy
//
// type Policy =
//     { TieBreakPolicy: Policy.TieBreakPolicy }
//
// type IChooseOneSolver =
//     abstract member Compare: policy: Policy.ChooseOnePolicy -> a: ParticipantResult -> b: ParticipantResult -> int
//
// type Error =
//     | ChooseOneSolverNotProvided
//     | InvalidPosition of Position: int
//
// let sequenceResults (xs: Result<Participant list, Error> list) =
//     xs
//     |> List.fold
//         (fun acc e ->
//             match acc, e with
//             | Error x, _ -> Error x
//             | _, Error x -> Error x
//             | Ok acc, Ok items -> Ok(acc @ items))
//         (Ok [])

/// Wynik z pełnym rankingiem
// type RankedResults =
//     private
//         { Ranked: Participant list }
//
//     static member Create
//         (
//             participantResults: ParticipantResult list,
//             policy: Policy,
//             chooseOneSolver: IChooseOneSolver option
//         ) : Result<RankedResults, Error> =
//
//         let toId =
//             function
//             | Results.Individual individual ->
//                 Results.ParticipantResultId.IndividualId individual.IndividualId
//             | Results.Team team -> Results.ParticipantResultId.TeamId team.TeamId
//
//         let groups =
//             participantResults
//             |> List.groupBy (fun participantResult ->
//                 match participantResult with
//                 | Results.Individual individual -> individual.TotalPoints
//                 | Results.Team team -> team.TotalPoints)
//             |> List.sortByDescending (fun (pts, _) -> let (Results.Points v) = pts in v)
//             |> List.map snd
//
//         match policy.TieBreakPolicy with
//         | RankedResults.Policy.ExAequo RankedResults.Policy.AddOneAfterExAequo ->
//             let folded =
//                 groups
//                 |> List.fold
//                     (fun acc grp ->
//                         match acc with
//                         | Error e -> Error e
//                         | Ok(accEntries, processed) ->
//                             let rank = processed + 1
//
//                             match RankedResults.Position.tryCreate rank with
//                             | Error _ -> Error(RankedResults.Error.InvalidPosition rank)
//                             | Ok pos ->
//                                 let entries =
//                                     grp
//                                     |> List.map (fun pr -> { Id = toId pr; Position = pos }: RankedResults.Participant)
//
//                                 Ok(accEntries @ entries, processed + List.length grp))
//                     (Ok([], 0))
//
//             match folded with
//             | Error e -> Error e
//             | Ok(entries, _) -> Ok { Ranked = entries }
//         | RankedResults.Policy.ExAequo RankedResults.Policy.ContinueNormallyAfterExAequo ->
//             groups
//             |> List.mapi (fun i grp ->
//                 match RankedResults.Position.tryCreate (i + 1) with
//                 | Error _ -> Error(RankedResults.Error.InvalidPosition(i + 1))
//                 | Ok pos ->
//                     Ok(
//                         grp
//                         |> List.map (fun pr -> { Id = toId pr; Position = pos }: RankedResults.Participant)
//                     ))
//             |> RankedResults.sequenceResults
//             |> Result.map (fun lst -> { Ranked = lst })
//         | RankedResults.Policy.ChooseOne chooseOnePolicy ->
//             match chooseOneSolver with
//             | Some solver ->
//                 let buildResults =
//                     participantResults
//                     |> List.sortWith (fun a b -> solver.Compare chooseOnePolicy a b)
//                     |> List.mapi (fun i pr ->
//                         match RankedResults.Position.tryCreate (i + 1) with
//                         | Ok pos -> Ok [ ({ Id = toId pr; Position = pos }: RankedResults.Participant) ]
//                         | Error _ -> Error(RankedResults.Error.InvalidPosition(i + 1)))
//                     |> RankedResults.sequenceResults
//
//                 buildResults |> Result.map (fun lst -> { Ranked = lst })
//             | None -> Error RankedResults.Error.ChooseOneSolverNotProvided

type ExAequoPolicy =
    | None
    | AddMoreThanOne
    | AddOne

type RankedResults = RankedResults of Map<Position, ParticipantResult.Id list>

type IRankedResultsFactory =
    abstract member Create: ParticipantResults: ParticipantResult list * RoundIndex: RoundIndex option -> RankedResults
