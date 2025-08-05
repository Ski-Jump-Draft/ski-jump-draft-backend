using App.Util;

namespace App.Application.ReadModel.CrudQuery;

public interface IGameWorldHillQuery
{
    Task<IEnumerable<GameWorldHillDto>> GetAllAsync();
    ValueTask<GameWorldHillInGameRecordsDto> GetInGameRecordsByIdAsync(Guid hillId);
}

/// Monthly record: First tuple's element indicates the month, and the second indicates the year
public record GameWorldHillInGameRecordsDto(
    double? Global,
    Dictionary<DateOnly, double> Daily,
    Dictionary<(Util.Month, int), double> Monthly);

public record GameWorldHillDto(
    Guid Id,
    string Location,
    string CountryId,
    string CountryCode,
    double KPoint,
    double HsPoint,
    GameWorldHillInGameRecordsDto InGameRecords);