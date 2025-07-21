using System.Collections.Concurrent;
using App.Application.Abstractions;
using App.Application.Util;

namespace App.Application.Factory.Impl.HillMapping;

public class
    InMemoryGameHillMapping : InMemoryBiDirectionalIdMap<Domain.GameWorld.HillId, Domain.Game.Hill.Id>,
    IGameHillMapping;

public class
    InMemoryCompetitionHillMapping : InMemoryBiDirectionalIdMap<Domain.Game.Hill.Id, Domain.Competition.HillModule.Id>,
    ICompetitionHillMapping;