using System.Collections.Immutable;
using System.Text.Json;
using App.Application.Acl;
using App.Application.Game.GameCompetitions;
using App.Application.Mapping;
using App.Application.Service;
using App.Application.Utility;
using App.Domain.GameWorld;

namespace App.Application.Policy.DraftPicker;

public class BestPicker(
    IGameJumperAcl gameJumperAcl,
    ICompetitionJumperAcl competitionJumperAcl,
    IJumpers gameWorldJumpersRepository,
    IGameCompetitionResultsArchive gameCompetitionResultsArchive,
    PreDraftPositionsService preDraftPositionsService,
    IRandom random,
    IMyLogger logger,
    IJumpers jumpers)
    : IDraftPicker, IDraftPassPicker
{
    private Dictionary<Guid, List<int>> _preDraftPositionsByJumper = new();

    public async Task<Guid> Pick(Domain.Game.Game game, CancellationToken ct = default)
    {
        ClearPreDraftPositions();
        var gameWorldJumpers = await RetrieveGameWorldJumpersFromDraftPicks(game, ct);
        var preDraftCompetitionResults = await RetrievePreDraftCompetitionResultsOrThrow(game, ct);
        var maxPosition = CalculateMaxPositionFromResults(preDraftCompetitionResults);
        logger.Debug("Max position: " + maxPosition);
        await UpdatePreDraftPositionsByJumper(game);
        var averagePositionByJumper = CreateAveragePositions(preDraftCompetitionResults);
        var ratingByGameJumper = CalculateGameJumperRatings(gameWorldJumpers, averagePositionByJumper, maxPosition);
        await LogRatingsWithNames(ratingByGameJumper, ct);
        return DrawJumper(ratingByGameJumper);
    }

    private async Task LogRatingsWithNames(Dictionary<Guid, double> ratingByGameJumper, CancellationToken ct)
    {
        var ratingsWithNames = new Dictionary<string, double>();
        foreach (var kvp in ratingByGameJumper)
        {
            var gameWorldJumperId = gameJumperAcl.GetGameWorldJumper(kvp.Key).Id;
            var gameWorldJumper = (await jumpers.GetById(JumperId.NewJumperId(gameWorldJumperId), ct)).Value;
            var nameAndSurname = $"{gameWorldJumper.Name.Item} {gameWorldJumper.Surname.Item}";
            ratingsWithNames[nameAndSurname] = kvp.Value;
        }

        logger.Info("ratingByGameJumper = " +
                    JsonSerializer.Serialize(ratingsWithNames, new JsonSerializerOptions { WriteIndented = true }));
    }

    private void ClearPreDraftPositions()
    {
        _preDraftPositionsByJumper.Clear();
    }

    private async Task<IEnumerable<Jumper>> RetrieveGameWorldJumpersFromDraftPicks(Domain.Game.Game game,
        CancellationToken ct)
    {
        var availableGameJumpers = game.AvailableDraftPicks;
        var gameWorldJumpers =
            await availableGameJumpers.ToGameWorldJumpers(gameJumperAcl, gameWorldJumpersRepository, ct);
        return gameWorldJumpers;
    }

    private async Task<ImmutableList<ArchiveCompetitionResultsDto>> RetrievePreDraftCompetitionResultsOrThrow(
        Domain.Game.Game game, CancellationToken ct)
    {
        var preDraftCompetitionResults =
            (await gameCompetitionResultsArchive.GetPreDraftResultsAsync(game.Id.Item, ct))?.ToImmutableList();
        if (preDraftCompetitionResults is null)
        {
            throw new Exception($"Game {game.Id} does not have pre-draft results in archive.");
        }

        return preDraftCompetitionResults;
    }

    private static int CalculateMaxPositionFromResults(
        ImmutableList<ArchiveCompetitionResultsDto> preDraftCompetitionResults)
    {
        var maxPosition = preDraftCompetitionResults.First().JumperResults.Where(record => record.Points != 0).ToList()
            .Count;
        return maxPosition;
    }

    private async Task UpdatePreDraftPositionsByJumper(Domain.Game.Game game)
    {
        _preDraftPositionsByJumper =
            (await preDraftPositionsService.GetPreDraftPositions(game.Id.Item)).PositionsByGameJumper;
    }

    private Dictionary<Guid, double> CalculateGameJumperRatings(IEnumerable<Jumper> gameWorldJumpers,
        Dictionary<Guid, double> averagePositionByJumper,
        int maxPosition)
    {
        Dictionary<Guid, double> gameJumperRating = new();
        foreach (var gameWorldJumper in gameWorldJumpers)
        {
            var gameJumperId = gameJumperAcl.GetGameJumper(gameWorldJumper.Id.Item).Id;
            var averagePosition = averagePositionByJumper[gameJumperId];
            gameJumperRating[gameJumperId] = CalculateRating(gameWorldJumper, maxPosition, averagePosition);
        }

        return gameJumperRating;
    }

    private Dictionary<Guid, double> CreateAveragePositions(
        IEnumerable<ArchiveCompetitionResultsDto> competitionResults)
    {
        var averagePosition = new Dictionary<Guid, double>();
        foreach (var results in competitionResults)
        {
            foreach (var result in results.JumperResults)
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

        logger.Debug($"averagePosition = {
            JsonSerializer.Serialize(averagePosition, new JsonSerializerOptions { WriteIndented = true })}");


        return averagePosition;
    }

    // private static double CalculatePositionsAverage(IEnumerable<double> positions, double p)
    // {
    //     var list = positions.ToList();
    //     return list.Count switch
    //     {
    //         0 => 0,
    //         1 => list.First(),
    //         _ => Math.Abs(p) < 1e-9
    //             ? Math.Exp(list.Average(Math.Log))
    //             : Math.Pow(list.Average(x => Math.Pow(x, p)), 1.0 / p)
    //     };
    // }

    private static double CalculatePositionsAverage(IEnumerable<double> positions, double p)
    {
        var list = positions.ToList();
        if (list.Count == 0) return 0;
        if (list.Count == 1) return list.First();

        double baseAvg = Math.Abs(p) < 1e-9
            ? Math.Exp(list.Average(Math.Log))
            : Math.Pow(list.Average(x => Math.Pow(x, p)), 1.0 / p);

        // --- volatility bonus ---
        var mean = list.Average();
        var variance = list.Select(x => Math.Pow(x - mean, 2)).Average();
        var stdDev = Math.Sqrt(variance);

        const double gamma = 0.37; // współczynnik – jak mocno nagradza rozrzut
        var effectiveAvg = baseAvg - gamma * stdDev;
        if (effectiveAvg < 1) effectiveAvg = 1; // zabezpieczenie

        return effectiveAvg;
    }


    private double CalculateRating(Jumper gameWorldJumper, int maxPosition, double averagePosition)
    {
        const double rootN = 1;
        const double positionImpactFactor = 0.6;
        logger.Debug($"max position: {maxPosition}, average position: {averagePosition}, rootN: {rootN}");

        var rating = (maxPosition - averagePosition).NthRoot(rootN) * positionImpactFactor;
        logger.Debug($"rating: {rating}");

        var takeoff = JumperModule.BigSkillModule.value(gameWorldJumper.Takeoff);
        var flight = JumperModule.BigSkillModule.value(gameWorldJumper.Flight);
        var takeoffAndFlightAverage = (takeoff + flight) / 2;
        const double alpha = 3; // alpha = X oznacza: różnica 1 w danym skillu odpowiada różnicy X średniej pozycji
        rating += takeoffAndFlightAverage * alpha;
        logger.Debug($"rating2: {rating}");

        const double beta = 17.5; // im większe, tym bardziej "winner-takes-all"
        rating = Math.Pow(rating, beta);
        logger.Debug($"rating3: {rating}");
        const decimal divider = 1e28m;
        return rating / (double)divider;
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