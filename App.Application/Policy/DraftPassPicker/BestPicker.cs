using System.Collections.Immutable;
using System.Text.Json;
using App.Application.Acl;
using App.Application.Game.GameCompetitions;
using App.Application.Mapping;
using App.Application.Service;
using App.Application.Utility;
using App.Domain.GameWorld;

namespace App.Application.Policy.DraftPassPicker;

public class BestPicker(
    IGameJumperAcl gameJumperAcl,
    ICompetitionJumperAcl competitionJumperAcl,
    IJumpers gameWorldJumper,
    IGameCompetitionResultsArchive gameCompetitionResultsArchive,
    PreDraftPositionsService preDraftPositionsService,
    IRandom random,
    IMyLogger logger)
    : IDraftPassPicker
{
    private Dictionary<Guid, List<int>> _preDraftPositionsByJumper = new();

    public async Task<Guid> Pick(Domain.Game.Game game, CancellationToken ct = default)
    {
        ClearPreDraftPositions();
        var gameWorldJumpers = await RetrieveGameWorldJumpersFromDraftPicks(game, ct);
        var preDraftCompetitionResults = RetrievePreDraftCompetitionResultsOrThrow(game);
        var maxPosition = CalculateMaxPositionFromResults(preDraftCompetitionResults);
        UpdatePreDraftPositionsByJumper(game);
        var averagePositionByJumper = CreateAveragePositions(preDraftCompetitionResults);
        var ratingByGameJumper = CalculateGameJumperRatings(gameWorldJumpers, averagePositionByJumper, maxPosition);
        return DrawJumper(ratingByGameJumper);
    }


    private void ClearPreDraftPositions()
    {
        _preDraftPositionsByJumper.Clear();
    }

    private async Task<IEnumerable<Jumper>> RetrieveGameWorldJumpersFromDraftPicks(Domain.Game.Game game,
        CancellationToken ct)
    {
        var availableGameJumpers = game.AvailableDraftPicks;
        var gameWorldJumpers = await availableGameJumpers.ToGameWorldJumpers(gameJumperAcl, gameWorldJumper, ct);
        return gameWorldJumpers;
    }

    private ImmutableList<CompetitionResultsDto> RetrievePreDraftCompetitionResultsOrThrow(Domain.Game.Game game)
    {
        var preDraftCompetitionResults =
            gameCompetitionResultsArchive.GetPreDraftResults(game.Id.Item)?.ToImmutableList();
        if (preDraftCompetitionResults is null)
        {
            throw new Exception($"Game {game.Id} does not have pre-draft results in archive.");
        }

        return preDraftCompetitionResults;
    }

    private static int CalculateMaxPositionFromResults(ImmutableList<CompetitionResultsDto> preDraftCompetitionResults)
    {
        var maxPosition = preDraftCompetitionResults.First().Results.Where(record => record.Points != 0).ToList().Count;
        return maxPosition;
    }

    private void UpdatePreDraftPositionsByJumper(Domain.Game.Game game)
    {
        _preDraftPositionsByJumper = preDraftPositionsService.GetPreDraftPositions(game.Id.Item).PositionsByGameJumper;
    }


    private static Dictionary<Guid, double> CalculateGameJumperRatings(IEnumerable<Jumper> gameWorldJumpers,
        Dictionary<Guid, double> averagePositionByJumper,
        int maxPosition)
    {
        Dictionary<Guid, double> gameJumperRating = new();
        foreach (var gameWorldJumper in gameWorldJumpers)
        {
            var averagePosition = averagePositionByJumper[gameWorldJumper.Id.Item];
            gameJumperRating[gameWorldJumper.Id.Item] = CalculateRating(gameWorldJumper, maxPosition, averagePosition);
        }

        return gameJumperRating;
    }

    private Dictionary<Guid, double> CreateAveragePositions(IEnumerable<CompetitionResultsDto> competitionResults)
    {
        var averagePosition = new Dictionary<Guid, double>();
        foreach (var results in competitionResults)
        {
            foreach (var result in results.Results)
            {
                var gameJumperId = competitionJumperAcl.GetGameJumper(result.CompetitionJumperId).Id;
                var positions = _preDraftPositionsByJumper[gameJumperId];
                if (positions.Count == 0)
                {
                    throw new Exception($"GameJumper {gameJumperId} has no saved positions in pre-draft.");
                }

                const double p = 0.5;

                averagePosition[gameJumperId] =
                    CalculatePositionsAverage(positions.Select(intPosition => (double)intPosition).ToList(), p: p);
            }
        }

        return averagePosition;
    }

    private static double CalculatePositionsAverage(IEnumerable<double> positions, double p)
    {
        var list = positions.ToList();
        if (list.Count == 0) return 0;

        return Math.Abs(p) < 1e-9
            ? Math.Exp(list.Average(Math.Log))
            : Math.Pow(list.Average(x => Math.Pow(x, p)), 1.0 / p);
    }


    private static double CalculateRating(Jumper gameWorldJumper, int maxPosition, double averagePosition)
    {
        const double rootN = 1.5;
        var rating = (maxPosition - averagePosition).NthRoot(rootN);
        var takeoff = JumperModule.BigSkillModule.value(gameWorldJumper.Takeoff);
        var flight = JumperModule.BigSkillModule.value(gameWorldJumper.Flight);
        var takeoffAndFlightAverage = (takeoff + flight) / 2;
        const double alpha = 1.5; // alpha = 1 oznacza: różnica 1 w danym skillu odpowiada różnicy 1 średniej pozycji
        rating += takeoffAndFlightAverage * alpha;
        return rating;
    }

    private Guid DrawJumper(Dictionary<Guid, double> ratingByGameJumper)
    {
        var totalWeight = ratingByGameJumper.Values.Sum();
        if (totalWeight <= 0)
            throw new Exception("All ratings are non-positive, cannot pick.");

        logger.Debug($"BestPicker: totalWeight = {totalWeight}");

        var randomNumber = random.NextDouble() * totalWeight;
        logger.Debug($"BestPicker: randomNumber = {randomNumber}");
        logger.Debug($"BestPicker: ratingByGameJumper = {ratingByGameJumper}");
        var serializedRatings = JsonSerializer.Serialize(
            ratingByGameJumper.OrderBy(kvp => kvp.Value),
            new JsonSerializerOptions { WriteIndented = true });

        logger.Debug($"BestPicker: ratingByGameJumper = {serializedRatings}");

        double cumulative = 0;
        foreach (var kvp in ratingByGameJumper)
        {
            cumulative += kvp.Value;
            if (randomNumber <= cumulative)
            {
                return kvp.Key;
            }
        }

        throw new Exception("Should not be reached.");
    }
}