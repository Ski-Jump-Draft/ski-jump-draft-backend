namespace App.Domain.SimpleCompetition

module Competitor =
    type Id = Id of System.Guid

module Team =
    type Id = Id of System.Guid

type CompetitorStatus =
    | Active
    | DidNotStart
    | Disqualified of DisqualificationReason

type Competitor =
    private
        { Id: Competitor.Id
          TeamId: Team.Id
          Status: CompetitorStatus }

    static member Create id teamId =
        { Id = id
          TeamId = teamId
          Status = CompetitorStatus.Active }

    member this.Disqualify reason =
        { this with
            Status = CompetitorStatus.Disqualified reason }

    member this.ReportDidNotStart() =
        { this with
            Status = CompetitorStatus.DidNotStart }

type Team =
    private
        { Id: Team.Id
          Competitors: Competitor list }

    member this.Create id competitors = { Id = id; Competitors = competitors }
