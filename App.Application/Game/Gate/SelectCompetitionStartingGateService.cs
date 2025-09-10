using Jumper = App.Domain.Competition.Jumper;

namespace App.Application.Game.Gate;

public interface ISelectGameStartingGateService
{
    Task<int> Select(IEnumerable<Jumper> competitionJumpers,
        Domain.Competition.Hill competitionHill, CancellationToken ct = default);
}

public class SelectCompetitionStartingGateService(
    IGameStartingGateSelectorFactory startingGateSelectorFactory) : ISelectGameStartingGateService
{
    public async Task<int> Select(IEnumerable<Jumper> competitionJumpers,
        Domain.Competition.Hill competitionHill, CancellationToken ct = default)
    {
        var gateSelectorContext =
            new GameStartingGateSelectorContext();
        var startingGateSelector =
            await startingGateSelectorFactory.CreateForCompetition(competitionJumpers, competitionHill, ct);
        return startingGateSelector.Select(gateSelectorContext);
    }
}