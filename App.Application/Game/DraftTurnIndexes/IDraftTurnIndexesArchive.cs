namespace App.Application.Game.DraftTurnIndexes;

public interface IDraftTurnIndexesArchive
{
    Task<List<DraftTurnIndexesDto>> GetAsync(Guid gameId);
    Task SetFixedAsync(Guid gameId, List<DraftFixedTurnIndexDto> fixedTurnIndexesDtos);
    Task AddRandomAsync(Guid gameId, Guid gamePlayerId, int turnIndex, int whichPickIndex);
}

public record DraftFixedTurnIndexDto(Guid gamePlayerId, int FixedTurnIndex);

public record DraftTurnIndexesDto(Guid gamePlayerId, int? FixedTurnIndex, List<int>? RandomTurnIndexes);