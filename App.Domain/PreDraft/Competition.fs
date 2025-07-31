namespace App.Domain.PreDraft.Competitions

open App.Domain

module Hill =
    type Id = Id of System.Guid

type Hill = { Id: Hill.Id }

module Competition =
     type Id = App.Domain.Competition.Id.Id
     //type Id = Id of System.Guid

     type Settings =
         { //HillId: Hill.Id
           CompetitionEnginePluginId: string
           EngineRawOptions: Map<string, obj> }

// type Competition =
//     { Id: Competition.Id
//       CompetitionId: App.Domain.Competition.Id.Id }

// module Competition =
//     
