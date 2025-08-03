namespace App.Domain.PreDraft.Competition

module Hill =
    type Id = Id of System.Guid

type Hill = { Id: Hill.Id }


type Id = App.Domain.Competition.Id.Id

type Settings =
    { CompetitionEnginePluginId: string
      EngineRawOptions: Map<string, obj> }
