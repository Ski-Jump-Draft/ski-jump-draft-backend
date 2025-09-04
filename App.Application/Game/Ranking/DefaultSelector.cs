using App.Application.Acl;
using App.Application.Game.DraftPicks;
using App.Domain.Game;

namespace App.Application.Game.Ranking;

public class DefaultSelector(IDraftPicksArchive draftPicksArchive, ICompetitionJumperAcl competitionJumperAcl)
    : IGameRankingFactorySelector
{
    public IGameRankingFactory Select(RankingPolicy rankingPolicy)
    {
        return rankingPolicy switch
        {
            { IsClassic: true } => new ClassicGameRankingFactory(draftPicksArchive, competitionJumperAcl),
            // { IsPodiumAtAllCosts: true } => new PodiumAtAllCosts(competitionJumperAcl), TODO
            _ => throw new UnsupportedPolicyException(rankingPolicy),
        };
    }
}

public class UnsupportedPolicyException(RankingPolicy policy, string? message = null) : Exception(message);