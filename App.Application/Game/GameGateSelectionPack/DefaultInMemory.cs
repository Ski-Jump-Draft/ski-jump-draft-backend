using System.Collections.Immutable;
using App.Application.Acl;
using App.Application.Game.GameSimulationPack;
using App.Application.Mapping;
using App.Application.Policy.GameGateSelector;
using App.Application.Utility;
using App.Domain.GameWorld;
using App.Domain.Simulation;
using Jumper = App.Domain.Competition.Jumper;

namespace App.Application.Game.GameGateSelectionPack;

public class DefaultInMemory(
    ICompetitionJumperAcl competitionJumperAcl,
    IGameJumperAcl gameJumperAcl,
    IGameSimulationPack gameSimulationPack,
    IJuryBraveryFactory juryBraveryFactory,
    IJumpers gameWorldJumpersRepository,
    IMyLogger logger) : IGameGateSelectionPack
{
    public async Task<GameGateSelectionPack> GetForCompetition(Guid gameId, IEnumerable<Jumper> jumpers,
        Domain.Competition.Hill hill,
        CancellationToken ct)
    {
        var simulationPack = gameSimulationPack.GetFor(gameId);
        var gameJumpers = jumpers.ToGameJumpers(competitionJumperAcl, gameId);
        var gameWorldJumpers = await gameJumpers.ToGameWorldJumpers(gameJumperAcl, gameWorldJumpersRepository, ct);
        var simulationJumpers =
            gameWorldJumpers.ToSimulationJumpers(form: jumper => JumperModule.LiveFormModule.value(jumper.LiveForm))
                .ToImmutableList();
        var simulationHill = hill.ToSimulationHill();
        var juryBravery = juryBraveryFactory.Create();
        logger.Info($"Creating iterative simulated starting gate selector with {juryBravery.ToString()
        } jury bravery. Will use {simulationJumpers.Count} jumpers.");
        var iterativeSimulatedStartingGateSelector =
            CreateIterativeSimulated(simulationPack, simulationJumpers, simulationHill, juryBravery);
        return new GameGateSelectionPack(iterativeSimulatedStartingGateSelector);
    }

    private IterativeSimulated CreateIterativeSimulated(GameSimulationPack.GameSimulationPack simulationPack,
        IEnumerable<Domain.Simulation.Jumper> simulationJumpers,
        Domain.Simulation.Hill simulationHill, JuryBravery juryBravery = JuryBravery.Medium)
    {
        return new IterativeSimulated(simulationPack.JumpSimulator, simulationPack.WeatherEngine, juryBravery,
            simulationJumpers, simulationHill);
    }
}