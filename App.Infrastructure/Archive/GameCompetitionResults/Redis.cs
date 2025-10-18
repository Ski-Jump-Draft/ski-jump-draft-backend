using System.Text.Json;
using App.Application.Acl;
using App.Application.Game.GameCompetitions;
using App.Application.Messaging.Notifiers;
using RedisRepository = App.Infrastructure.Repository.Game;
using StackExchange.Redis;

namespace App.Infrastructure.Archive.GameCompetitionResults;

public class Redis(
    IConnectionMultiplexer redis,
    ICompetitionJumperAcl competitionJumperAcl,
    IGameJumperAcl gameJumperAcl) : IGameCompetitionResultsArchive
{
    private readonly IDatabase _db = redis.GetDatabase();

    private static string LivePattern => $"game:live";
    private static string LiveKey(Guid id) => $"{LivePattern}:{id}";
    private static string ArchivePattern => $"game:archive";
    private static string ArchiveKey(Guid id) => $"{ArchivePattern}:{id}";

    public async Task ArchivePreDraftAsync(Guid gameId, ArchiveCompetitionResultsDto archiveCompetitionResults,
        CancellationToken ct)
    {
        var gameDto = await GetGameDto(gameId, searchInArchive: false);
        if (gameDto is null)
            throw new GameNotFoundException();
        if (gameDto.PreDraft is null)
            throw new Exception($"PreDraftDto is null (status={gameDto.Status})");

        var endedCompetitionResults = RedisEndedCompetitionFromArchived(archiveCompetitionResults);

        var preDraftEndedCompetitions = gameDto.PreDraft.EndedCompetitions is not null
            ? gameDto.PreDraft.EndedCompetitions.ToList()
            : [];
        preDraftEndedCompetitions.Add(endedCompetitionResults);

        var newPreDraft = gameDto.PreDraft with { EndedCompetitions = preDraftEndedCompetitions };

        var newGame = gameDto with { PreDraft = newPreDraft };
        await _db.StringSetAsync(LiveKey(gameId), JsonSerializer.Serialize(newGame));
    }

    public async Task<List<ArchiveCompetitionResultsDto>?> GetPreDraftResultsAsync(Guid gameId, CancellationToken ct)
    {
        // TODO: Czy gameDto.PreDraft nie jest nullem po domenowej fazie Draftu?
        var gameDto = await GetGameDto(gameId, searchInArchive: true);
        if (gameDto?.PreDraft is null) return null;
        var endedCompetitions = gameDto.PreDraft.EndedCompetitions ?? [];
        var archiveCompetitions =
            endedCompetitions.Select(endedCompetition => ArchivedCompetitionResultsFromRedis(gameId, endedCompetition));

        return archiveCompetitions.ToList();
    }

    public async Task ArchiveMainAsync(Guid gameId, ArchiveCompetitionResultsDto archiveCompetitionResults,
        CancellationToken ct)
    {
        var gameDto = await GetGameDto(gameId, searchInArchive: false);
        if (gameDto is null)
            throw new GameNotFoundException();
        var redisEndedCompetitionResults = RedisEndedCompetitionFromArchived(archiveCompetitionResults);
        var newGame = gameDto with { EndedMainCompetition = redisEndedCompetitionResults };
        await _db.StringSetAsync(LiveKey(gameId), JsonSerializer.Serialize(newGame));
    }

    public async Task<ArchiveCompetitionResultsDto?> GetMainResultsAsync(Guid gameId, CancellationToken ct)
    {
        var gameDto = await GetGameDto(gameId, searchInArchive: true);
        return gameDto?.EndedMainCompetition != null
            ? ArchivedCompetitionResultsFromRedis(gameId, gameDto.EndedMainCompetition)
            : null;
    }

    private async Task<RedisRepository.GameDto?> GetGameDto(Guid gameId, bool searchInArchive)
    {
        var liveJson = await _db.StringGetAsync(LiveKey(gameId));
        if (liveJson.HasValue)
            return Deserialize(liveJson);

        if (!searchInArchive) return null;

        var archiveJson = await _db.StringGetAsync(ArchiveKey(gameId));
        return archiveJson.HasValue ? Deserialize(archiveJson) : null;

        static RedisRepository.GameDto Deserialize(RedisValue json) =>
            JsonSerializer.Deserialize<RedisRepository.GameDto>(json!)
            ?? throw new Exception("Failed to deserialize game JSON");
    }

    private RedisRepository.EndedCompetitionDto RedisEndedCompetitionFromArchived(
        ArchiveCompetitionResultsDto archiveCompetitionResults)
    {
        var endedCompetitionResults = new RedisRepository.EndedCompetitionDto(
            archiveCompetitionResults.JumperResults.Select(archiveJumperResult =>
            {
                var jumperResults = archiveJumperResult.Jumps.Select((jumpResult, roundIndex) =>
                {
                    var roundResult = new RedisRepository.CompetitionRoundResultDto(jumpResult.Id,
                        jumpResult.CompetitionJumperId, roundIndex, jumpResult.Distance, jumpResult.Points,
                        jumpResult.Judges,
                        jumpResult.JudgePoints, jumpResult.WindCompensation, jumpResult.WindAverage,
                        jumpResult.Gate, jumpResult.GateCompensation, jumpResult.TotalCompensation);
                    return roundResult;
                }).ToList();

                return new RedisRepository.CompetitionResultDto(archiveJumperResult.CompetitionJumperId,
                    archiveJumperResult.Bib,
                    archiveJumperResult.Points, archiveJumperResult.Rank, jumperResults);
            }).ToList());
        return endedCompetitionResults;
    }

    private ArchiveCompetitionResultsDto ArchivedCompetitionResultsFromRedis(Guid gameId,
        RedisRepository.EndedCompetitionDto endedCompetition)
    {
        var jumperResults = endedCompetition.Results.Select(endedCompetitionJumperResult =>
        {
            var (gameJumperId, gameWorldJumperId) =
                GetGameJumperAndGameWorldJumper(gameId, endedCompetitionJumperResult.CompetitionJumperId);
            var jumpResults = endedCompetitionJumperResult.RoundResults.Select(endedCompetitionRoundResult =>
                    new ArchiveJumpResult(endedCompetitionRoundResult.JumpResultId,
                        endedCompetitionRoundResult.CompetitionJumperId, endedCompetitionRoundResult.Distance,
                        endedCompetitionRoundResult.Points, endedCompetitionRoundResult.Judges,
                        endedCompetitionRoundResult.JudgePoints,
                        endedCompetitionRoundResult.WindCompensation, endedCompetitionRoundResult.WindAverage,
                        endedCompetitionRoundResult.Gate,
                        endedCompetitionRoundResult.GateCompensation,
                        endedCompetitionRoundResult.TotalCompensation))
                .ToList();
            var bib = endedCompetitionJumperResult.Bib;
            var jumperResult = new ArchiveJumperResult(gameWorldJumperId, gameJumperId,
                endedCompetitionJumperResult.CompetitionJumperId, endedCompetitionJumperResult.Rank, bib,
                endedCompetitionJumperResult.Total, jumpResults);
            return jumperResult;
        }).ToList();
        return new ArchiveCompetitionResultsDto(jumperResults);
    }

    private (Guid, Guid) GetGameJumperAndGameWorldJumper(Guid gameId, Guid competitionJumperId)
    {
        var gameJumperId = competitionJumperAcl.GetGameJumper(gameId, competitionJumperId).GameJumperId;
        var gameWorldJumperId = gameJumperAcl.GetGameWorldJumper(gameJumperId).GameWorldJumperId;
        return (gameJumperId, gameWorldJumperId);
    }
}

public class GameNotFoundException(string? message = null) : Exception(message);