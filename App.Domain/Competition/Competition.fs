namespace App.Domain.Competition

open App.Domain
open App.Domain.GameWorld
open App.Domain.Shared

module Competition =
    type Id = Id of System.Guid
    
    type Settings = { Rules: Rules.Config }

open Competition
type Competition =
    { Id: Competition.Id
      HillId: GameWorld.Hill.Id
      Settings: Settings }
    
    static member Create (idGen: IGuid) (hillId: Hill.Id) rulesConfig =
        { Id = Competition.Id(idGen.NewGuid())
          HillId = hillId
          Settings = { Rules = rulesConfig } }