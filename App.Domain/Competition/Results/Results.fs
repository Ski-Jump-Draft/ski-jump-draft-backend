module App.Domain.Competition.ResultsModule

open System
open App.Domain.Competition
open App.Domain.Competition.Phase
open App.Domain.Competition.Results
open App.Domain.Competition.Results.ParticipantResult

// [<Struct>]
// type Id = Id of Guid

type Error =
    | IndividualParticipantHasManyIndividualResults of IndividualResult list
    | IndividualParticipantBelongsToManyTeamResults of TeamParticipant.Id list

module private Helpers =

    let updateRoundPoints roundIndex totalPoints pointsByRound =
        Map.change roundIndex (fun _ -> Some totalPoints) pointsByRound

    let replaceParticipantResult predicate replacement participantResults =
        participantResults
        |> List.map (fun participantResult ->
            if predicate participantResult then
                replacement
            else
                participantResult)

    let duplicatesBy selector items =
        items
        |> Seq.groupBy selector
        |> Seq.filter (fun (_, group) -> Seq.length group > 1)
        |> Seq.collect snd
        |> Seq.toList

    let latestTotalPoints pointsByRound =
        pointsByRound |> Map.toSeq |> Seq.tryLast |> Option.map snd

let private validateParticipantResults (participantResults: ParticipantResult list) : Error list =

    let individualDuplicateErrors =
        participantResults
        |> List.choose (fun participantResult ->
            match participantResult.Details with
            | Details.IndividualResultDetails individualResult -> Some individualResult
            | _ -> None)
        |> Helpers.duplicatesBy (fun individualResult -> individualResult.IndividualParticipantId)
        |> function
            | [] -> []
            | duplicates -> [ Error.IndividualParticipantHasManyIndividualResults duplicates ]

    let individualToTeamMappings =
        participantResults
        |> List.collect (fun participantResult ->
            match participantResult.Details with
            | Details.TeamResultDetails teamResult ->
                teamResult.MemberResults
                |> List.map (fun individualResult -> individualResult.IndividualParticipantId, teamResult.TeamId)
            | _ -> [])

    let teamMembershipErrors =
        individualToTeamMappings
        |> Helpers.duplicatesBy fst
        |> List.map snd
        |> List.distinct
        |> function
            | [] -> []
            | conflictingTeams -> [ Error.IndividualParticipantBelongsToManyTeamResults conflictingTeams ]

    individualDuplicateErrors @ teamMembershipErrors

type ParticipantResults = ParticipantResult list

type Results =
    { ParticipantResults: ParticipantResults }

    static member Empty = { ParticipantResults = [] }

    static member FromState participantResults =
        match validateParticipantResults participantResults with
        | [] -> Ok { ParticipantResults = participantResults }
        | errors -> Error errors

    member this.GetTeamResult(teamId: TeamParticipant.Id) : ParticipantResult =
        let matching =
            this.ParticipantResults
            |> List.filter (function
                | { Details = Details.TeamResultDetails tr } when tr.TeamId = teamId -> true
                | _ -> false)

        match matching with
        | [ pr ] -> pr
        | [] -> invalidOp $"Team result for {teamId} not found"
        | _ -> invalidOp $"Team {teamId} appears multiple times in participant results"

    member this.GetIndividualResult(individualId: IndividualParticipant.Id) : ParticipantResult =
        let matching =
            this.ParticipantResults
            |> List.filter (function
                | { Details = Details.IndividualResultDetails ir } when ir.IndividualParticipantId = individualId -> true
                | _ -> false)

        match matching with
        | [ pr ] -> pr
        | [] -> invalidOp $"Individual result for {individualId} not found"
        | _ -> invalidOp $"Individual {individualId} appears in multiple participant results"

    member this.MapTotalPointsByRound roundIndexOpt =
        this.ParticipantResults
        |> List.choose (fun participantResult ->
            let totalPointsOption =
                match roundIndexOpt with
                | Some roundIndex -> Map.tryFind roundIndex participantResult.TotalPoints
                | None -> Helpers.latestTotalPoints participantResult.TotalPoints

            Option.map (fun pts -> participantResult.Id, pts) totalPointsOption)
        |> Map.ofList

    member this.GetTeamTotalScoreByRound
        (teamId: TeamParticipant.Id, roundIndex: RoundIndex)
        : ParticipantResult.TotalPoints =

        this.GetTeamResult teamId
        |> fun teamPr -> teamPr.TotalPoints |> Map.find roundIndex

    member this.GetIndividualTotalScoreByRound
        (individualId: IndividualParticipant.Id, roundIndex: RoundIndex)
        : ParticipantResult.TotalPoints =

        this.GetIndividualResult individualId
        |> fun individualPr -> individualPr.TotalPoints |> Map.find roundIndex

    member this.RegisterIndividualJump
        (
            jumpResult: JumpResult,
            newTotalPoints: ParticipantResult.TotalPoints,
            potentialParticipantResultId: ParticipantResult.Id
        ) =

        let individualId = jumpResult.IndividualParticipantId
        let roundIndex = jumpResult.RoundIndex

        let updatedParticipantResults =
            match
                this.ParticipantResults
                |> List.tryFind (function
                    | { Details = Details.IndividualResultDetails ir } when ir.IndividualParticipantId = individualId -> true
                    | _ -> false)
            with
            | None ->
                let individualResult =
                    { IndividualParticipantId = individualId
                      JumpResults = [ jumpResult ] }

                let newParticipantResult =
                    { Id = potentialParticipantResultId
                      TotalPoints = Map.empty |> Helpers.updateRoundPoints roundIndex newTotalPoints
                      Details = Details.IndividualResultDetails individualResult }

                newParticipantResult :: this.ParticipantResults

            | Some existingParticipantResult ->
                let updatedIndividualResult =
                    match existingParticipantResult.Details with
                    | Details.IndividualResultDetails ir ->
                        { ir with
                            JumpResults = jumpResult :: ir.JumpResults }
                    | _ -> invalidOp "Mismatched participant result type"

                let updatedParticipantResult =
                    { existingParticipantResult with
                        Details = Details.IndividualResultDetails updatedIndividualResult
                        TotalPoints =
                            existingParticipantResult.TotalPoints
                            |> Helpers.updateRoundPoints roundIndex newTotalPoints }

                Helpers.replaceParticipantResult
                    (fun pr -> pr.Id = existingParticipantResult.Id)
                    updatedParticipantResult
                    this.ParticipantResults

        { ParticipantResults = updatedParticipantResults }

    member this.RegisterTeamJump
        (
            jumpResult: JumpResult,
            teamId: TeamParticipant.Id,
            newIndividualTotalPoints: ParticipantResult.TotalPoints,
            newTeamTotalPoints: ParticipantResult.TotalPoints,
            potentialIndividualResultId: ParticipantResult.Id,
            potentialTeamResultId: ParticipantResult.Id
        ) =

        let individualId = jumpResult.IndividualParticipantId
        let roundIndex = jumpResult.RoundIndex

        let conflictingTeams =
            this.ParticipantResults
            |> List.choose (function
                | { Details = Details.TeamResultDetails teamResult } when
                    teamResult.ContainsIndividualResult individualId && teamResult.TeamId <> teamId
                    ->
                    Some teamResult.TeamId
                | _ -> None)

        match conflictingTeams with
        | _ :: _ -> invalidOp $"Individual {individualId} already appears in teams {conflictingTeams}"
        | [] -> ()

        let afterIndividualUpdate =
            (this.RegisterIndividualJump(jumpResult, newIndividualTotalPoints, potentialIndividualResultId))
                .ParticipantResults

        let updatedParticipantResults =
            match
                afterIndividualUpdate
                |> List.tryFind (function
                    | { Details = Details.TeamResultDetails tr } when tr.TeamId = teamId -> true
                    | _ -> false)
            with
            | None ->
                let newIndividualResult =
                    { IndividualParticipantId = individualId
                      JumpResults = [ jumpResult ] }

                let newTeamResult =
                    { TeamId = teamId
                      MemberResults = [ newIndividualResult ] }

                let newParticipantResult =
                    { Id = potentialTeamResultId
                      TotalPoints = Map.empty |> Helpers.updateRoundPoints roundIndex newTeamTotalPoints
                      Details = Details.TeamResultDetails newTeamResult }

                newParticipantResult :: afterIndividualUpdate

            | Some existingParticipantResult ->
                let teamResult =
                    match existingParticipantResult.Details with
                    | Details.TeamResultDetails tr -> tr
                    | _ -> invalidOp "Mismatched participant result type"

                let updatedMemberResults =
                    match teamResult.IndividualResultOf individualId with
                    | None ->
                        { IndividualParticipantId = individualId
                          JumpResults = [ jumpResult ] }
                        :: teamResult.MemberResults
                    | Some existingIndividualResult ->
                        let updatedIndividualResult =
                            { existingIndividualResult with
                                JumpResults = jumpResult :: existingIndividualResult.JumpResults }

                        Helpers.replaceParticipantResult
                            (fun ir -> ir.IndividualParticipantId = individualId)
                            updatedIndividualResult
                            teamResult.MemberResults

                let updatedTeamResult =
                    { teamResult with
                        MemberResults = updatedMemberResults }

                let updatedParticipantResult =
                    { existingParticipantResult with
                        Details = Details.TeamResultDetails updatedTeamResult
                        TotalPoints =
                            existingParticipantResult.TotalPoints
                            |> Helpers.updateRoundPoints roundIndex newTeamTotalPoints }

                Helpers.replaceParticipantResult
                    (fun pr -> pr.Id = existingParticipantResult.Id)
                    updatedParticipantResult
                    afterIndividualUpdate

        { ParticipantResults = updatedParticipantResults }
