using App.Application.Acl;
using App.Application.Extensions;
using App.Application.Game.GameCompetitions;
using App.Domain.Competition;

namespace App.Application.Mapping;

public static class CompetitionClassificationMappers
{
    public static ArchiveCompetitionResultsDto ToGameCompetitionResultsArchiveDto(
        this IEnumerable<Classification.JumperClassificationResult> jumperClassificationResults,
        Guid gameId,
        // Func<Guid, Guid> gameJumperByCompetitionJumper, Func<Guid, Guid> gameWorldJumperByGameJumper,
        IGameJumperAcl gameJumperAcl, ICompetitionJumperAcl competitionJumperAcl,
        Func<Guid, int> getBibByCompetitionJumperId)
    {
        return new ArchiveCompetitionResultsDto(jumperClassificationResults.Select(jumperClassificationResult =>
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

                return new ArchiveJumpResult(
                    jumpResult.Id.Item,
                    jumperClassificationResult.JumperId.Item,
                    JumpModule.DistanceModule.value(jumpResult.Jump.Distance),
                    TotalPointsModule.value(jumpResult.TotalPoints),
                    JumpModule.JudgesModule.value(jumpResult.Jump.JudgeNotes),
                    judgePoints,
                    windPoints,
                    jumpResult.Jump.Wind.ToDouble(),
                    jumpResult.Jump.Gate.Item,
                    gatePoints,
                    JumpResultModule.TotalCompensationModule.value(jumpResult.TotalCompensation));
            });
            var competitionJumperGuid = jumperClassificationResult.JumperId.Item;
            var gameJumperGuid = competitionJumperAcl.GetGameJumper(gameId, competitionJumperGuid).GameJumperId;
            var gameWorldJumperGuid = gameJumperAcl.GetGameWorldJumper(gameJumperGuid).GameWorldJumperId;
            // var gameJumperGuid = gameJumperByCompetitionJumper.Invoke(competitionJumperGuid);
            // var gameWorldJumperGuid = gameWorldJumperByGameJumper.Invoke(gameJumperGuid);

            return new ArchiveJumperResult(gameWorldJumperGuid, gameJumperGuid,
                competitionJumperGuid,
                Classification.PositionModule.value(jumperClassificationResult.Position),
                getBibByCompetitionJumperId(jumperClassificationResult.JumperId.Item),
                jumperClassificationResult.Points.Item, jumpRecords.ToList());
        }).ToList());
    }
}