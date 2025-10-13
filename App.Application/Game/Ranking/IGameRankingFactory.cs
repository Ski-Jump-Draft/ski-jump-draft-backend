using Microsoft.FSharp.Collections;

namespace App.Application.Game.Ranking;

public interface IGameRankingFactory
{
    Task<Domain.Game.Ranking> Create(Domain.Game.Game game, CancellationToken ct);
}

public record SimpleGameRankingJumperDto(
    Guid GameJumperId,
    Guid GameWorldJumperId,
    string FisCountryCode,
    List<int> TrainingRanks,
    int MainCompetitionRank,
    bool JumpedFurthestInMainCompetition)
{
    public bool IsInTop(int rank) => MainCompetitionRank <= rank;
}

public record GamePlayerDto(Guid GamePlayerId, List<SimpleGameRankingJumperDto> PickedJumpers);

public static class SimpleGameRankingExtensions
{
    public static List<GamePlayerDto> CreateGamePlayerDtosForSimpleGameRankingFactoryInput(this Domain.Game.Game game)
    {
        throw new NotImplementedException();
    }

    public static Domain.Game.Ranking ToGameRanking(this SimpleGameRanking simpleRanking)
    {
        var dictionary = simpleRanking.PlayerRecords.ToDictionary(
            keySelector: gameRankingRecord => Domain.Game.PlayerId.NewPlayerId(gameRankingRecord.GamePlayerId),
            elementSelector: gameRankingRecord =>
                Domain.Game.RankingModule.PointsModule.create(gameRankingRecord.Points).Value);
        var fSharpMap = MapModule.OfSeq(dictionary.Select(kv => Tuple.Create(kv.Key, kv.Value)));
        var ranking = Domain.Game.Ranking.Create(fSharpMap);
        return ranking;
    }
}

public record SimpleGameRankingRecord(Guid GamePlayerId, int Points);

public record SimpleGameRanking(List<SimpleGameRankingRecord> PlayerRecords);

public interface ISimpleGameRankingFactory
{
    SimpleGameRanking Create(List<GamePlayerDto> players);
}