namespace App.Domain.PreDraft.Competitions

open App.Domain
open App.Domain.GameWorld

module Competition =
    type Id = Id of System.Guid

    type Settings =
        { HillId: HillId
          CompetitionEnginePluginId: string //Engine.Template.Id
          EngineRawOptions: Map<string, obj> }

type Competition =
    { Id: Competition.Id
      CompetitionId: App.Domain.Competition.Id.Id }
