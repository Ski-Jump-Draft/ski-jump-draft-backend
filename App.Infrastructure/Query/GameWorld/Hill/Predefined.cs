using App.Application.ReadModel.CrudQuery;
using App.Util;

namespace App.Infrastructure.Query.GameWorld.Hill;

public class Predefined(
    IReadOnlyCollection<Domain.GameWorld.Hill> gameWorldHills,
    IGameWorldCountryQuery gameWorldCountryQuery
) : IGameWorldHillQuery
{
    private readonly IReadOnlyDictionary<Guid, Domain.GameWorld.Hill> _hills =
        gameWorldHills.ToDictionary(h => h.Id_.Item);

    public async Task<IEnumerable<GameWorldHillDto>> GetAllAsync()
    {
        var countryCache = new Dictionary<Guid, string>();
        var tasks = _hills.Values.Select(async hill =>
        {
            var records = await GetInGameRecordsByIdAsync(hill.Id_.Item).ConfigureAwait(false);
            if (!countryCache.TryGetValue(hill.CountryId_.Item, out var code))
            {
                code = (await gameWorldCountryQuery.GetCountryCodeByIdAsync(hill.CountryId_).ConfigureAwait(false))!
                    .CountryCode;
                countryCache[hill.CountryId_.Item] = code;
            }

            return new GameWorldHillDto(
                hill.Id_.Item,
                hill.Location_.Item,
                hill.CountryId_.Item.ToString(),
                code,
                Domain.GameWorld.HillTypes.KPointModule.value(hill.KPoint_),
                Domain.GameWorld.HillTypes.HsPointModule.value(hill.HsPoint_),
                records
            );
        });
        return await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    public ValueTask<GameWorldHillInGameRecordsDto> GetInGameRecordsByIdAsync(Guid hillId)
    {
        if (!_hills.TryGetValue(hillId, out var hill))
            return ValueTask.FromResult<GameWorldHillInGameRecordsDto>(null!);

        var inGameRecords = hill.InGameRecords_;
        double? global = inGameRecords.Global.IsSome() ? inGameRecords.Global.Value.Distance.Value : null;
        var daily = inGameRecords.Daily.ToDictionary(
            kv => DateOnly.FromDateTime(kv.Key.Item),
            kv => kv.Value.Distance.Value
        );
        var monthly = inGameRecords.Monthly.ToDictionary(
            kv => (MonthExtensions.FromInt(kv.Key.Number_), kv.Key.Year_),
            kv => kv.Value.Distance.Value
        );

        return ValueTask.FromResult(new GameWorldHillInGameRecordsDto(global, daily, monthly));
    }
}