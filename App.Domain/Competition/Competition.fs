namespace App.Domain.Competition

open App.Domain.Shared
open App.Domain.Shared.Ids

module Competition =
    type Settings = { Rules: Rules.Config }

open Competition
type Competition =
    { Id: CompetitionId
      HillId: HillId
      Settings: Settings }
    
    static member Create idGen hillId rulesConfig =
        { Id = Id.newCompetitionId idGen
          HillId = hillId
          Settings = { Rules = rulesConfig } }