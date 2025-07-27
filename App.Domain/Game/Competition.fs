namespace App.Domain.Game

open App.Domain

module Competition =
    [<Struct>]
    type Id = Id of System.Guid

    type Settings =
        { HillId: Hill.Id
          CompetitionEnginePluginId: string //Engine.Template.Id
          EngineRawOptions: Map<string, obj> }

type Competition =
    { Id: Competition.Id
      CompetitionId: App.Domain.Competition.Id.Id }

    static member Create id competitionId =
        { Id = id
          CompetitionId = competitionId }
