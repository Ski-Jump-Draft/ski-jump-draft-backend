using App.Application.Acl;
using App.Application.Mapping;
using App.Application.Policy.GameGateSelector;
using App.Domain.GameWorld;
using App.Domain.Simulation;
using Hill = App.Domain.Competition.Hill;
using HillId = App.Domain.Competition.HillId;
using Jumper = App.Domain.Competition.Jumper;
using JumperId = App.Domain.Competition.JumperId;

namespace App.Application.Game.Gate;

public class IterativeSimulatedFactory(
    IJumpSimulator jumpSimulator,
    IWeatherEngine weatherEngine,
    JuryBravery juryBravery,
    ICompetitionJumperAcl competitionJumperAcl,
    IGameJumperAcl gameJumperAcl,
    ICompetitionHillAcl competitionHillAcl,
    IJumpers gameWorldJumpersRepository) : IGameStartingGateSelectorFactory
{
    public ICompetitionHillAcl CompetitionHillAcl { get; } = competitionHillAcl;

    public async Task<IGameStartingGateSelector> CreateForCompetition(IEnumerable<Jumper> jumpers, Hill hill,
        CancellationToken ct = default)
    {
        var gameJumpers = jumpers.ToGameJumpers(competitionJumperAcl);
        var gameWorldJumpers = await gameJumpers.ToGameWorldJumpers(gameJumperAcl, gameWorldJumpersRepository, ct);
        var simulationJumpers = gameWorldJumpers.ToSimulationJumpers();
        
        var simulationHill = hill.ToSimulationHill();
        return new IterativeSimulated(jumpSimulator, weatherEngine, juryBravery, simulationJumpers, simulationHill);
    }
}