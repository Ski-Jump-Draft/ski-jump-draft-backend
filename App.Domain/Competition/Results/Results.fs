module App.Domain.Competition.ResultsModule

open System
open App.Domain.Competition
open App.Domain.Competition.Phase
open App.Domain.Competition.Results.ResultObjects
open App.Domain.Competition.Results.ResultObjects.ParticipantResult

[<Struct>]
type Id = Id of Guid

type Error =
    | IndividualParticipantHasManyIndividualResults of IndividualResult list
    | IndividualParticipantBelongsToManyTeamResults of Team.Id list

// type Event =
//     | IndividualJumpAdded of ResultsId: Id * Timestamp: EventTimestamp * JumpScoreId: JumpScore.Id
//     | TeamJumpAdded of ResultsId: Id * Timestamp: EventTimestamp * JumpScoreId: JumpScore.Id

[<AutoOpen>]
module private Helpers =
    let setRoundPoints round points map = Map.add round points map

    let replaceParticipant predicate replacement list =
        list |> List.map (fun x -> if predicate x then replacement else x)

let private validateParticipantResults (prs: ParticipantResult list) : Error list =
    /// 1. ten sam zawodnik w wielu wynikach indywidualnych
    let individualErrors =
        prs
        |> List.choose (function
            | { Details = Details.IndividualResultDetails ir } -> Some ir
            | _ -> None)
        |> List.groupBy (fun ir -> ir.IndividualId)
        |> List.choose (fun (_, group) ->
            if group.Length > 1 then
                Some(Error.IndividualParticipantHasManyIndividualResults group)
            else
                None)

    /// 2. zawodnik przypisany do więcej niż jednej drużyny
    let teamErrors =
        prs
        |> List.choose (function
            | { Details = Details.TeamResultDetails tr } ->
                tr.MemberResults |> List.map (fun ir -> ir.IndividualId, tr.TeamId) |> Some
            | _ -> None)
        |> List.collect id
        |> List.groupBy fst
        |> List.choose (fun (_, xs) ->
            let teamIds = xs |> List.map snd |> List.distinct

            if teamIds.Length > 1 then
                Some(Error.IndividualParticipantBelongsToManyTeamResults teamIds)
            else
                None)

    individualErrors @ teamErrors

type ResultsState = ParticipantResult list

type Results =
    { Id: Id
      ParticipantResults: ResultsState }

    static member Empty id = { Id = id; ParticipantResults = [] }

    static member FromState (id: Id) (participantResults: ResultsState) : Result<Results, Error list> =
        match validateParticipantResults participantResults with
        | [] ->
            Ok
                { Id = id
                  ParticipantResults = participantResults }
        | errors -> Error errors


    member this.GetTeamResult(teamId: Team.Id) : TeamResult =
        match
            this.ParticipantResults
            |> List.tryPick (function
                | { Details = Details.TeamResultDetails teamResult } when teamResult.TeamId = teamId -> Some teamResult
                | _ -> None)
        with
        | Some teamResult -> teamResult
        | None -> failwith $"Team result for {teamId} not found"

    member this.GetIndividualResult(individualId: IndividualParticipant.Id) : IndividualResult =
        let matching =
            this.ParticipantResults
            |> List.choose (function
                | { Details = Details.IndividualResultDetails individualResult } when
                    individualResult.IndividualId = individualId
                    ->
                    Some individualResult
                | _ -> None)

        match matching with
        | [ individualResult ] -> individualResult
        | [] -> failwith $"Individual result for {individualId} not found"
        | many -> raise (InvalidOperationException $"Individual in many individual results: {many}")

    // member this.MapTotalPointsByRound
    //     (roundIndex: RoundIndex)
    //     : Map<ParticipantResult.Id, ParticipantResult.TotalPoints> =
    //     this.ParticipantResults
    //     |> List.choose (fun pr ->
    //         match pr.TotalPoints |> Map.tryFind roundIndex with
    //         | Some pts -> Some(pr.Id, pts)
    //         | None -> None)
    //     |> Map.ofList

    member this.MapTotalPointsByRound
        (roundIndexOpt: RoundIndex option)
        : Map<ParticipantResult.Id, ParticipantResult.TotalPoints> =

        this.ParticipantResults
        |> List.choose (fun pr ->
            let totalPointsByRound = pr.TotalPoints

            let pointsOpt =
                match roundIndexOpt with
                | Some roundIndex -> Map.tryFind roundIndex totalPointsByRound
                | None ->
                    match Map.toList totalPointsByRound with
                    | [] -> None
                    | xs -> xs |> List.maxBy fst |> snd |> Some

            pointsOpt |> Option.map (fun pts -> pr.Id, pts)
        )
        |> Map.ofList

    member this.GetTeamTotalScoreByRound(teamId: Team.Id, roundIndex: RoundIndex) : ParticipantResult.TotalPoints =
        let pr =
            this.ParticipantResults
            |> List.find (function
                | { Details = Details.TeamResultDetails teamResult } when teamResult.TeamId = teamId -> true
                | _ -> false)

        pr.TotalPoints |> Map.find roundIndex

    member this.GetIndividualTotalScoreByRound
        (individualId: IndividualParticipant.Id, roundIndex: RoundIndex)
        : ParticipantResult.TotalPoints =
        let pr =
            this.ParticipantResults
            |> List.find (function
                | { Details = Details.IndividualResultDetails individualResult } when
                    individualResult.IndividualId = individualId
                    ->
                    true
                | _ -> false)

        pr.TotalPoints |> Map.find roundIndex

    member this.RegisterIndividualJump
        (
            jumpResult: JumpResult,
            newTotalPoints: ParticipantResult.TotalPoints,
            potentialParticipantResultId: ParticipantResult.Id
        ) : Results =
        let individualId = jumpResult.IndividualParticipantId
        let roundIndex = jumpResult.RoundIndex

        let (updatedList: ParticipantResult list) =
            match
                this.ParticipantResults
                |> List.tryFind (function
                    | { Details = Details.IndividualResultDetails individualResult } when
                        individualResult.IndividualId = individualId
                        ->
                        true
                    | _ -> false)
            with
            | None ->
                let individualResult =
                    { IndividualId = individualId
                      JumpResults = [ jumpResult ] }

                let participantResult =
                    { Id = potentialParticipantResultId
                      TotalPoints = Map.empty |> setRoundPoints roundIndex newTotalPoints
                      Details = ParticipantResult.Details.IndividualResultDetails individualResult }

                participantResult :: this.ParticipantResults
            | Some existingParticipantResult ->
                let individualResult =
                    match existingParticipantResult.Details with
                    | Details.IndividualResultDetails individualResult -> individualResult
                    | _ -> failwith "Mismatched participant result kind"

                let updatedIr =
                    { individualResult with
                        JumpResults = jumpResult :: individualResult.JumpResults }

                let updatedParticipantResult =
                    { existingParticipantResult with
                        Details = ParticipantResult.Details.IndividualResultDetails updatedIr
                        TotalPoints =
                            existingParticipantResult.TotalPoints
                            |> setRoundPoints roundIndex newTotalPoints }

                replaceParticipant
                    (fun participantResult -> participantResult.Id = existingParticipantResult.Id)
                    updatedParticipantResult
                    this.ParticipantResults

        { this with
            ParticipantResults = updatedList }

    member this.RegisterTeamJump
        (
            jumpResult: JumpResult,
            teamId: Team.Id,
            newIndividualTotalPoints: ParticipantResult.TotalPoints,
            newTeamTotalPoints: ParticipantResult.TotalPoints,
            potentialIndividualResultId: ParticipantResult.Id,
            potentialTeamResultId: ParticipantResult.Id
        ) : Results =

        let individualId = jumpResult.IndividualParticipantId
        let roundIndex = jumpResult.RoundIndex

        // validation: participant cannot belong to other teams
        let otherTeams =
            this.ParticipantResults
            |> List.choose (function
                | { Details = Details.TeamResultDetails teamResult } when
                    teamResult.ContainsIndividualResult individualId && teamResult.TeamId <> teamId
                    ->
                    Some teamResult.TeamId
                | _ -> None)

        match otherTeams with
        | _ :: _ -> raise (InvalidOperationException $"Individual {individualId} already in teams {otherTeams}")
        | [] -> ()

        // update or create individual ParticipantResult
        let resultsAfterIndividualUpdate =
            (this.RegisterIndividualJump(jumpResult, newIndividualTotalPoints, potentialIndividualResultId))
                .ParticipantResults

        // update or create team ParticipantResult
        let updatedParticipantResults =
            match
                resultsAfterIndividualUpdate
                |> List.tryFind (function
                    | { Details = Details.TeamResultDetails teamResult } when teamResult.TeamId = teamId -> true
                    | _ -> false)
            with
            | None ->
                let newIndividualResult =
                    { IndividualId = individualId
                      JumpResults = [ jumpResult ] }

                let newTeamRes =
                    { TeamId = teamId
                      MemberResults = [ newIndividualResult ] }

                let newParticipantResult =
                    { Id = potentialTeamResultId
                      TotalPoints = Map.empty |> setRoundPoints roundIndex newTeamTotalPoints
                      Details = Details.TeamResultDetails newTeamRes }

                newParticipantResult :: resultsAfterIndividualUpdate

            | Some existingParticipantResult ->
                let teamResult =
                    match existingParticipantResult.Details with
                    | Details.TeamResultDetails teamResult -> teamResult
                    | _ -> failwith "Mismatched participant result kind"

                let updatedMemberResults =
                    match teamResult.IndividualResultOf individualId with
                    | None ->
                        { IndividualId = individualId
                          JumpResults = [ jumpResult ] }
                        :: teamResult.MemberResults
                    | Some individualResult ->
                        let updatedIr =
                            { individualResult with
                                JumpResults = jumpResult :: individualResult.JumpResults }

                        replaceParticipant
                            (fun individualResult -> individualResult.IndividualId = individualId)
                            updatedIr
                            teamResult.MemberResults

                let updatedTeamRes =
                    { teamResult with
                        MemberResults = updatedMemberResults }

                let updatedParticipantResult =
                    { existingParticipantResult with
                        Details = ParticipantResult.Details.TeamResultDetails updatedTeamRes
                        TotalPoints =
                            existingParticipantResult.TotalPoints
                            |> setRoundPoints roundIndex newTeamTotalPoints }

                replaceParticipant
                    (fun pr -> pr.Id = existingParticipantResult.Id)
                    updatedParticipantResult
                    resultsAfterIndividualUpdate

        { this with
            ParticipantResults = updatedParticipantResults }
