namespace Game.Core.Domain.Competitions

open Game.Core.Domain.Shared
open Game.Core.Domain.Shared.Ids

module Competition =
    type Settings = { Rules: RulesConfig }

    type Definition =
        { Id: CompetitionId
          HillId: HillId
          //InitialWind: WindMap
          Settings: Settings }

    let create hillId rulesConfig =
        { Id = Id.newCompetitionId ()
          HillId = hillId
          Settings = { Rules = rulesConfig } }
