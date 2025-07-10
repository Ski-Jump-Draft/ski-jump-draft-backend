namespace App.Domain.Competition

open System

module IndividualParticipant =
    type Id = Id of Guid

type IndividualParticipant = { Id: IndividualParticipant.Id }

module Team =
    type Id = Id of Guid
    type Error = | NoMembers

type CompetitionEntity =
    | IndividualParticipantEntity of IndividualParticipant
    | TeamEntity of Team

and Team =
    { Id: Team.Id
      Members: CompetitionEntity list }

    static member Create id members =
        if members |> List.isEmpty then
            Error(Team.Error.NoMembers)
        else
            Ok { Id = id; Members = members }

    member this.HierarchyDepth =
        let rec depth =
            function
            | IndividualParticipantEntity _ -> 1u
            | TeamEntity tg ->
                let childDepths = tg.Members |> List.map depth

                1u
                + (if List.isEmpty childDepths then
                       0u
                   else
                       List.max childDepths)

        depth (TeamEntity this)

    member this.MembersCount = this.Members |> List.length

    member this.HaveOnlyIndividuals: bool =
        this.Members
        |> List.forall (function
            | IndividualParticipantEntity _ -> true
            | TeamEntity _ -> false)

    member this.ContainsIndividual(individualId: IndividualParticipant.Id) : bool =
        this.Members
        |> List.exists (fun participant ->
            match participant with
            | IndividualParticipantEntity individual -> individual.Id = individualId
            | TeamEntity team -> team.ContainsIndividual individualId)

type CompetitionEntityId =
    | IndividualParticipantEntityId of IndividualParticipant.Id
    | TeamEntityId of Team.Id

// module Participant =
//     type IndividualId = IndividualId of Guid
//     type TeamId = TeamId of Guid
//
//     type Individual = { Id: IndividualId }
//
//     module Team =
//         type Error = | NoMembers
//
//     type Participant =
//         | Individual of Individual
//         | Team of Team
//
//     and Team =
//         private
//             { Id: TeamId
//               Members: Participant list }
//
//         static member Create id members =
//             if members |> List.isEmpty then
//                 Error(Team.Error.NoMembers)
//             else
//                 Ok { Id = id; Members = members }
//
//         member this.HierarchyDepth =
//             let rec depth =
//                 function
//                 | Individual _ -> 1u
//                 | Team tg ->
//                     let childDepths = tg.Members |> List.map depth
//
//                     1u
//                     + (if List.isEmpty childDepths then
//                            0u
//                        else
//                            List.max childDepths)
//
//             depth (Team this)
//
//         member this.MembersCount = this.Members |> List.length
//
//         member this.HaveOnlyIndividuals: bool =
//             this.Members
//             |> List.forall (function
//                 | Individual _ -> true
//                 | Team _ -> false)
//
//         member this.ContainsIndividual(individualId: IndividualId) : bool =
//             this.Members
//             |> List.exists (fun participant ->
//                 match participant with
//                 | Individual individual -> individual.Id = individualId
//                 | Team team -> team.ContainsIndividual individualId)
//
//
//     module Participant =
//         let rec ChildrenIds =
//             function
//             | Individual i -> [ i.Id ]
//             | Team t -> t.Members |> List.collect ChildrenIds
