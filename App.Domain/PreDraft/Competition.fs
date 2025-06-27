namespace App.Domain.PreDraft.Competitions

open App.Domain

module Competition =
    type Id = Id of System.Guid

type Competition =
    {
        Id: Competition.Id
        RulesConfig: Competitions.Rules.Rules
    }