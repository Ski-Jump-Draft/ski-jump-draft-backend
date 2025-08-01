using App.Util;

namespace App.Application.CompetitionEngine.Helper.HillMapping;

// public class
//     InMemoryGameHillMapping : InMemoryBiDirectionalIdMap<Domain.GameWorld.HillId, Domain.Game.Hill.Id>,
//     IGameHillMapping;

public class
    InMemoryPreDraftHillMapping : InMemoryBiDirectionalIdMap<Domain.GameWorld.HillTypes.Id,
        Domain.PreDraft.Competitions.HillModule.Id>,
    IPreDraftHillMapping;

// public class
//     InMemoryCompetitionHillMapping : InMemoryBiDirectionalIdMap<Domain.GameWorld.HillTypes.Id, Domain.Competition.HillModule.Id>,
//     ICompetitionHillMapping;

//
// public class
//     InMemoryPreDraftHillMapping : InMemoryBiDirectionalIdMap<Domain.Game.Hill.Id,
//         Domain.PreDraft.Competitions.HillModule.Id>,
//     IPreDraftHillMapping;
//
// public class
//     InMemoryCompetitionHillMapping : InMemoryBiDirectionalIdMap<Domain.Game.Hill.Id, Domain.Competition.HillModule.Id>,
//     ICompetitionHillMapping;