using App.Domain.Competitions;
//using App.Domain.Draft;
using App.Domain;
using App.Domain.Game;
using App.Domain.Competitions;

namespace App.Application.CSharp.Game.Utils.EndedGameResults;


public interface IRankingCreator
{
    GameModule.EndedGameResults.Ranking Create(Domain.Game.GameModule.Id gameId, Domain.Draft.Picks.Picks picks,
        Domain.Competition.Competition.Results competitionResults);
}