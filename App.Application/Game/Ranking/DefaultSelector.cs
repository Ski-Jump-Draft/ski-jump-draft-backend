using App.Application.Acl;
using App.Application.Game.DraftPicks;
using App.Application.Game.GameCompetitions;
using App.Application.Utility;
using App.Domain.Game;

namespace App.Application.Game.Ranking;

public class DefaultSelector(
    IDraftPicksArchive draftPicksArchive,
    IGameCompetitionResultsArchive gameCompetitionResultsArchive,
    ICompetitionJumperAcl competitionJumperAcl,
    IMyLogger logger)
    : IGameRankingFactorySelector
{
    public IGameRankingFactory Select(RankingPolicy rankingPolicy)
    {
        return rankingPolicy switch
        {
            { IsClassic: true } => new ClassicGameRankingFactory(draftPicksArchive, gameCompetitionResultsArchive,
                competitionJumperAcl, logger),
            { IsPodiumAtAllCosts: true } => new PodiumAtAllCostFactory(draftPicksArchive,
                gameCompetitionResultsArchive, competitionJumperAcl, logger),
            _ => throw new UnsupportedPolicyException(rankingPolicy),
        };
    }
}

public class UnsupportedPolicyException(RankingPolicy policy, string? message = null) : Exception(message);