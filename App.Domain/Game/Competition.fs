namespace App.Domain.Game

open App.Domain

module Competition =
    [<Struct>]
    type Id = Id of System.Guid

    // module Hill =
    //     type Id = Id of System.Guid

    type Settings =
        { //HillId: Hill.Id
          CompetitionEnginePluginId: string //Engine.Template.Id
          EngineRawOptions: Map<string, obj> }

type Competition =
    private
        { CompetitionId: App.Domain.Competition.Id.Id }

    static member Create competitionId = { CompetitionId = competitionId }
