namespace App.Application.Game.Ranking.Simple;

public class NewGeneration : ISimpleGameRankingFactory
{
    public SimpleGameRanking Create(List<GamePlayerDto> players)
    {
        var records = players.Select(gamePlayerDto =>
        {
            var points = CalculatePoints(gamePlayerDto);
            var record = new SimpleGameRankingRecord(gamePlayerDto.GamePlayerId, points);
            return record;
        });
        return new SimpleGameRanking(records.ToList());
    }

    private static int CalculatePoints(GamePlayerDto player)
    {
        var points = 0;
        var pickedJumpers = player.PickedJumpers;
        foreach (var pickedJumper in pickedJumpers)
        {
            var mainCompetitionRank = pickedJumper.MainCompetitionRank;
            bool IsInTop(int rank) => mainCompetitionRank <= rank;
            var countryIsOutsider = CountryIsOutsider(pickedJumper.FisCountryCode);

            if (countryIsOutsider && IsInTop(10))
            {
                points += 3;
            }

            points += mainCompetitionRank switch
            {
                1 => 10,
                2 => 9,
                3 => 8,
                4 => 7,
                5 => 6,
                > 5 and <= 10 => 5,
                > 10 and <= 20 => 3,
                > 20 and <= 30 => 1,
                _ => 0
            };

            var jumpedFurthestInMainCompetition = pickedJumper.JumpedFurthestInMainCompetition;
            if (jumpedFurthestInMainCompetition)
            {
                points += 1;
            }
        }

        var everyJumperIsInTop30 = pickedJumpers.All(pickedJumper => pickedJumper.IsInTop(30));
        var everyJumperIsOutsider = pickedJumpers.All(pickedJumper => CountryIsOutsider(pickedJumper.FisCountryCode));

        if (everyJumperIsInTop30)
        {
            points += 1;
        }

        if (everyJumperIsOutsider)
        {
            points += 3;
        }

        return points;
    }

    private static List<string> TopCountries => ["AUT", "POL", "GER", "JPN", "SLO", "NOR"];

    private static bool CountryIsOutsider(string fisCountryCode)
    {
        return !TopCountries.Contains(fisCountryCode);
    }
}