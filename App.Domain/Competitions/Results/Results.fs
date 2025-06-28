namespace App.Domain.Competitions

open System
open App.Domain.Shared.EventHelpers
open App.Domain.Time

module Results =
    [<Struct>]
    type Id = Id of Guid

    type Points = Points of decimal

    module JumpResult =
        type Id = Id of Guid

    type JumpResult =
        { Id: JumpResult.Id
          ParticipantId: Participant.IndividualId
          RoundIndex: RoundIndex
          Points: Points }

    type IndividualResult =
        { IndividualId: Participant.IndividualId
          JumpResults: JumpResult list
          TotalPoints: Points }

    type TeamResult =
        { TeamId: Participant.TeamId
          MemberResults: IndividualResult list
          TotalPoints: Points }

        member this.IndividualResultOf id =
            this.MemberResults |> List.tryFind (fun ir -> ir.IndividualId = id)

        member this.ContainsIndividualResult id =
            this.MemberResults |> List.exists (fun ir -> ir.IndividualId = id)

    type ParticipantResult =
        | Individual of IndividualResult
        | Team of TeamResult

    type ParticipantResultId =
        | IndividualId of Participant.IndividualId
        | TeamId of Participant.TeamId

    type Error =
        | IndividualParticipantHasManyIndividualResults of IndividualResult list
        | IndividualParticipantBelongsToManyTeamResults of Participant.TeamId list
        
    type Event =
        | IndividualJumpAdded of ResultsId: Id * Timestamp: EventTimestamp * JumpResultId: JumpResult.Id
        | TeamJumpAdded of ResultsId: Id * Timestamp: EventTimestamp * JumpResultId: JumpResult.Id

open Results

type Results =
    { Id: Results.Id
      ParticipantResults: ParticipantResult list }

    static member Empty id = { Id = id; ParticipantResults = [] }

    member this.RegisterJumpOfIndividualParticipant
        (individualId: Participant.IndividualId)
        (jumpResult: JumpResult)
        (newTotalPoints: Points)
        : Results =
        let exists =
            this.ParticipantResults
            |> List.exists (function
                | Individual individualResult when individualResult.IndividualId = individualId -> true
                | _ -> false)

        let updated =
            this.ParticipantResults
            |> List.map (function
                | Individual individualResult when individualResult.IndividualId = individualId ->
                    Individual
                        { individualResult with
                            JumpResults = individualResult.JumpResults @ [ jumpResult ]
                            TotalPoints = newTotalPoints }
                | participantResult -> participantResult)

        let final =
            if exists then
                updated
            else
                Individual
                    { IndividualId = individualId
                      JumpResults = [ jumpResult ]
                      TotalPoints = newTotalPoints }
                :: updated

        { this with ParticipantResults = final }

    member this.RegisterJumpOfTeamParticipant
        (individualId: Participant.IndividualId)
        (teamId: Participant.TeamId)
        (jumpResult: JumpResult)
        (newIndividualTotal: Points)
        (newTeamTotal: Points)
        : Results =
        let hasTeam =
            this.ParticipantResults
            |> List.exists (function
                | Team teamResult when teamResult.TeamId = teamId -> true
                | _ -> false)

        let updatedParticipantResults =
            this.ParticipantResults
            |> List.map (function
                | ParticipantResult.Team teamResult when teamResult.TeamId = teamId ->
                    let inTeam = teamResult.ContainsIndividualResult individualId

                    let members =
                        teamResult.MemberResults
                        |> List.map (fun individualResult ->
                            if individualResult.IndividualId = individualId then
                                { individualResult with
                                    JumpResults = individualResult.JumpResults @ [ jumpResult ]
                                    TotalPoints = newIndividualTotal }
                            else
                                individualResult)
                        |> fun individualResults ->
                            if inTeam then
                                individualResults
                            else
                                individualResults
                                @ [ { IndividualId = individualId
                                      JumpResults = [ jumpResult ]
                                      TotalPoints = newIndividualTotal } ]

                    ParticipantResult.Team
                        { teamResult with
                            MemberResults = members
                            TotalPoints = newTeamTotal }
                | participantResult -> participantResult)

        let final =
            if hasTeam then
                updatedParticipantResults
            else
                let ir =
                    { IndividualId = individualId
                      JumpResults = [ jumpResult ]
                      TotalPoints = newIndividualTotal }

                ParticipantResult.Team
                    { TeamId = teamId
                      MemberResults = [ ir ]
                      TotalPoints = newTeamTotal }
                :: updatedParticipantResults

        { this with ParticipantResults = final }

    member this.JumpResult (roundIndex: RoundIndex) (individualId: Participant.IndividualId) : JumpResult option =
        this.ParticipantResults
        |> List.collect (function
            | Individual individualResult when individualResult.IndividualId = individualId ->
                individualResult.JumpResults
            | Team teamResult ->
                teamResult.MemberResults
                |> List.filter (fun ir -> ir.IndividualId = individualId)
                |> List.collect (fun ir -> ir.JumpResults)
            | _ -> [])
        |> List.tryFind (fun jr -> jr.RoundIndex = roundIndex)

    member this.ContainsJumpForRound (roundIndex: RoundIndex) (individualId: Participant.IndividualId) : bool =
        this.JumpResult roundIndex individualId |> Option.isSome
