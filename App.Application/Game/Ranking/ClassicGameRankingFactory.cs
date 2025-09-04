using App.Application.Acl;
using App.Application.Game.DraftPicks;
using App.Domain.Competition;
using App.Domain.Game;
using Microsoft.FSharp.Collections;
using JumperId = App.Domain.Game.JumperId;
using RankingModule = App.Domain.Game.RankingModule;

namespace App.Application.Game.Ranking;

// TODO: Zrobić refactor i reuse. + PodiumAtAllCosts
public class ClassicGameRankingFactory(IDraftPicksArchive draftPicksArchive, ICompetitionJumperAcl competitionJumperAcl)
    : IGameRankingFactory
{
    public Task<Domain.Game.Ranking> Create(Domain.Game.Game game, CancellationToken ct)
    {
        if (!game.WaitsForEnd)
        {
            throw new Exception("Game must be ended to create a ranking");
        }

        var playerIds = PlayersModule.toIdsList(game.Players).ToList();
        var picks = draftPicksArchive.GetPicks(game.Id_.Item);
        var mainCompetitionClassification = game.CurrentCompetitionClassification.ToList();

        var mainCompetitionPositionByJumper = new Dictionary<JumperId, int>();
        foreach (var classificationResult in mainCompetitionClassification)
        {
            var competitionJumperId = classificationResult.JumperId;
            var gameJumperDto = competitionJumperAcl.GetGameJumper(competitionJumperId.Item);
            var gameJumperId = JumperId.NewJumperId(gameJumperDto.Id);
            mainCompetitionPositionByJumper.Add(gameJumperId,
                Classification.PositionModule.value(classificationResult.Position));
        }

        var mainCompetitionPositionsByPlayer = new Dictionary<PlayerId, List<int>>();
        foreach (var playerId in playerIds)
        {
            var positions = new List<int>();
            foreach (var playerPicks in picks)
            {
                if (playerPicks.Key.Equals(playerId))
                {
                    foreach (var pick in playerPicks.Value)
                    {
                        positions.Add(mainCompetitionPositionByJumper[pick]);
                    }
                }
            }

            mainCompetitionPositionsByPlayer.Add(playerId, positions);
        }

        var pointsByPlayer = new Dictionary<PlayerId, int>();
        foreach (var playerId in playerIds)
        {
            var pointsForPlayer = 0;
            var positions = mainCompetitionPositionsByPlayer[playerId];
            foreach (var position in positions)
            {
                if (position == 1) pointsForPlayer += 10;
                else if (position == 2) pointsForPlayer += 9;
                else if (position == 3) pointsForPlayer += 8;
                else if (position == 4) pointsForPlayer += 7;
                else if (position == 5) pointsForPlayer += 6;
                else if (position <= 10) pointsForPlayer += 5;
                else if (position <= 20) pointsForPlayer += 3;
                else if (position <= 30) pointsForPlayer += 1;
            }

            pointsByPlayer.Add(playerId, pointsForPlayer);
        }

        var fSharpRankingMap = new FSharpMap<PlayerId, RankingModule.Points>(
            pointsByPlayer.Select(idAndPoints =>
            {
                var points = RankingModule.PointsModule.create(idAndPoints.Value).Value;
                return Tuple.Create(idAndPoints.Key, points);
            })
        );

        return Task.FromResult(Domain.Game.Ranking.Create(fSharpRankingMap));
    }
}