using App.Application.Acl;
using App.Application.Mapping;
using App.Domain.GameWorld;

namespace App.Application.Game.Gate;

public interface ISelectGameStartingGateService
{
    Task<int> Select(Domain.Game.Game game, CancellationToken ct = default);
}

public class SelectGameStartingGateService(
    IHills gameWorldHillsRepository,
    IJumpers gameWorldJumpersRepository,
    IGameJumperAcl gameJumperAcl,
    ICompetitionHillAcl competitionHillAcl,
    IGameStartingGateSelector startingGateSelector) : ISelectGameStartingGateService
{
    public async Task<int> Select(Domain.Game.Game game, CancellationToken ct = default)
    {
        var gameWorldJumpers = await game.Jumpers.ToGameWorldJumpers(gameJumperAcl, gameWorldJumpersRepository, ct);
        var gameWorldHill = await game.Hill.Value.ToGameWorldHill(gameWorldHillsRepository, competitionHillAcl, ct: ct);
        var gateSelectorContext =
            new GameStartingGateSelectorContext(gameWorldJumpers.ToSimulationJumpers(),
                gameWorldHill.ToSimulationHill());
        return startingGateSelector.Select(gateSelectorContext);
    }
}