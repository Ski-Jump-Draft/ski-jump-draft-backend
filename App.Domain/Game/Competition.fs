namespace App.Domain.Game

open App.Domain

module Competition =
    [<Struct>]
    type Id = Id of System.Guid
    
type Competition =
    {
        Id: Competition.Id
        CompetitionId: Competitions.Competition.Id
    }
    
    static member Create id competitionId =
        {
            Id = id
            CompetitionId = competitionId
        }