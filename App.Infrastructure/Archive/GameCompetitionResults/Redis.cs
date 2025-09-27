using System.Text.Json;
using App.Application.Game.GameCompetitions;
using App.Application.Messaging.Notifiers;
using RedisRepository = App.Infrastructure.Repository.Game;
using StackExchange.Redis;

namespace App.Infrastructure.Archive.GameCompetitionResults;

public class Redis(IConnectionMultiplexer redis) : IGameCompetitionResultsArchive
{
    private readonly IDatabase _db = redis.GetDatabase();

    private static string LivePattern => $"game:live";
    private static string LiveKey(Guid id) => $"{LivePattern}:{id}";
    private static string ArchivePattern => $"game:archive";
    private static string ArchiveKey(Guid id) => $"{ArchivePattern}:{id}";

    public async Task ArchivePreDraftAsync(Guid gameId, CompetitionResultsDto competitionResults, CancellationToken ct)
    {
        var gameDto = await GetGameDto(gameId, searchInArchive: false);

        if (gameDto.PreDraft is null)
            throw new Exception($"PreDraftDto is null (status={gameDto.Status}, next={gameDto.NextStatus})");

        var endedCompetitionResults = new RedisRepository.EndedCompetitionDto(
            competitionResults.JumperResults.Select(archiveJumperResult =>
            {
                var jumperResults = archiveJumperResult.Jumps.Select((jumpResult, roundIndex) =>
                {
                    var roundResult = new RedisRepository.CompetitionRoundResultDto(jumpResult.Id,
                        jumpResult.CompetitionJumperId, roundIndex, jumpResult.Distance, jumpResult.Points,
                        jumpResult.Judges,
                        jumpResult.JudgePoints, jumpResult.WindCompensation, jumpResult.WindAverage,
                        jumpResult.GateCompensation, jumpResult.TotalCompensation);
                    return roundResult;
                }).ToList();

                return new RedisRepository.CompetitionResultDto(archiveJumperResult.CompetitionJumperId,
                    archiveJumperResult.Points, archiveJumperResult.Rank, jumperResults);
            }).ToList());

        var preDraftEndedCompetitions = gameDto.PreDraft.EndedCompetitions is not null
            ? gameDto.PreDraft.EndedCompetitions.ToList()
            : [];
        preDraftEndedCompetitions.Add(endedCompetitionResults);

        var newPreDraft = gameDto.PreDraft with { EndedCompetitions = preDraftEndedCompetitions };

        var newGame = gameDto with { PreDraft = newPreDraft };
        await _db.StringSetAsync(LiveKey(gameId), JsonSerializer.Serialize(newGame));
    }

    public async Task<List<CompetitionResultsDto>?> GetPreDraftResultsAsync(Guid gameId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public async Task ArchiveMainAsync(Guid gameId, CompetitionResultsDto competitionResults, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public async Task<CompetitionResultsDto?> GetMainResultsAsync(Guid gameId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    private async Task<RedisRepository.GameDto> GetGameDto(Guid gameId, bool searchInArchive)
    {
        var liveJson = await _db.StringGetAsync(LiveKey(gameId));
        if (liveJson.HasValue)
            return Deserialize(liveJson);

        if (!searchInArchive) throw new GameNotFoundException();

        var archiveJson = await _db.StringGetAsync(ArchiveKey(gameId));
        if (archiveJson.HasValue)
            return Deserialize(archiveJson);

        throw new GameNotFoundException();

        static RedisRepository.GameDto Deserialize(RedisValue json) =>
            JsonSerializer.Deserialize<RedisRepository.GameDto>(json!)
            ?? throw new Exception("Failed to deserialize game JSON");
    }
}

public class GameNotFoundException(string? message = null) : Exception(message);