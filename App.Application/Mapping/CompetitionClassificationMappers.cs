using App.Application.Acl;
using App.Application.Extensions;
using App.Application.Game.GameCompetitions;
using App.Domain.Competition;

namespace App.Application.Mapping;

public static class CompetitionClassificationMappers
{
    public static CompetitionResultsDto ToGameCompetitionResultsArchiveDto(
        this IEnumerable<Classification.JumperClassificationResult> jumperClassificationResults,
        // Func<Guid, Guid> gameJumperByCompetitionJumper, Func<Guid, Guid> gameWorldJumperByGameJumper,
        IGameJumperAcl gameJumperAcl, ICompetitionJumperAcl competitionJumperAcl,
        Func<Guid, int> getBibByCompetitionJumperId)
    {
        return new CompetitionResultsDto(jumperClassificationResults.Select(jumperClassificationResult =>
        {
            var jumpRecords = jumperClassificationResult.JumpResults.Select(jumpResult =>
            {
                double? judgePoints = jumpResult.JudgePoints.IsSome()
                    ? JumpResultModule.JudgePointsModule.value(jumpResult.JudgePoints.Value)
                    : null;
                double? windPoints = jumpResult.WindPoints.IsSome()
                    ? JumpResultModule.WindPointsModule.value(jumpResult.WindPoints.Value)
                    : null;
                double? gatePoints = jumpResult.GatePoints.IsSome()
                    ? JumpResultModule.GatePointsModule.value(jumpResult.GatePoints.Value)
                    : null;

                return new ResultJumpRecord(JumpModule.DistanceModule.value(jumpResult.Jump.Distance),
                    TotalPointsModule.value(jumpResult.TotalPoints),
                    JumpModule.JudgesModule.value(jumpResult.Jump.JudgeNotes),
                    judgePoints,
                    windPoints,
                    jumpResult.Jump.Wind.ToDouble(),
                    gatePoints,
                    JumpResultModule.TotalCompensationModule.value(jumpResult.TotalCompensation));
            });
            var competitionJumperGuid = jumperClassificationResult.JumperId.Item;
            var gameJumperGuid = competitionJumperAcl.GetGameJumper(competitionJumperGuid).Id;
            var gameWorldJumperGuid = gameJumperAcl.GetGameWorldJumper(gameJumperGuid).Id;
            // var gameJumperGuid = gameJumperByCompetitionJumper.Invoke(competitionJumperGuid);
            // var gameWorldJumperGuid = gameWorldJumperByGameJumper.Invoke(gameJumperGuid);

            return new ResultRecord(gameWorldJumperGuid, gameJumperGuid,
                competitionJumperGuid,
                Classification.PositionModule.value(jumperClassificationResult.Position),
                getBibByCompetitionJumperId(jumperClassificationResult.JumperId.Item),
                jumperClassificationResult.Points.Item, jumpRecords.ToList());
        }).ToList());
    }
}