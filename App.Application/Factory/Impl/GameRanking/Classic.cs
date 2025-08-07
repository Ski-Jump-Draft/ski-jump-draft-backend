using App.Application.Ext;
using App.Application.ReadModel.Projection;
using App.Domain.Game;
using Microsoft.FSharp.Collections;

namespace App.Application.Factory.Impl.GameRanking;

public class Classic(IGameCompetitionProjection gameCompetitionProjection, IGameDraftProjection gameDraftProjection)
    : Ranking.IGameRankingFactory
{
    public async Task<Ranking.GameRanking> Create(Id.Id gameId)
    {
        // Algorytm:
        // Za każdego Twojego w TOP 10 dostajesz +15 punktów. Za TOP30 dostajesz +5 punktów.  Za najdłuższy skok +5 punktow.

        // Czego potrzebuijemy>
        // Dictionary<Game.Participant, draftsubject
        // Dictionary draft subject, position
        // => Dictionary Game.Participant, list<position>
        // => Dictionary<GameParticipant, points>

        var draft = await gameDraftProjection.GetEndedDraftByGameIdAsync(gameId).AwaitOrWrapNullable(_ =>
            new InvalidOperationException("No ended Draft found for gameId: " + gameId + ""));
        var subjectsByGameParticipantId = draft.SubjectsByGameParticipantId;

        var (_, _, subjectPositions) = await gameCompetitionProjection
            .GetDraftSubjectPositionsByEndedGameIdAsync(gameId).AwaitOrWrapNullable(_ =>
                new InvalidOperationException("No PostDraft Competition found for gameId: " + gameId + ""));

        var positionsByGameParticipant = subjectsByGameParticipantId
            .ToDictionary(
                pair => pair.Key,
                pair => pair.Value
                    .Where(subjectPositions.ContainsKey)
                    .Select(subjectId => subjectPositions[subjectId])
            );

        var pointsByGameParticipant = positionsByGameParticipant
            .ToDictionary(
                kvp => kvp.Key,
                kvp =>
                {
                    var points = 0;
                    foreach (var position in kvp.Value)
                    {
                        switch (position)
                        {
                            case <= 3:
                                points += 20;
                                break;
                            case <= 10:
                                points += 10;
                                break;
                            case <= 30:
                                points += 3;
                                break;
                        }
                    }

                    return points;
                });

        var pointsByGameParticipantMap = MapModule.OfSeq(
            pointsByGameParticipant.Select(kvp =>
                Tuple.Create(kvp.Key, Ranking.GameRankingModule.PointsModule.tryCreate(kvp.Value).Value))
        );


        return Ranking.GameRanking.NewRanking(pointsByGameParticipantMap);
    }
}