using App.Application.Extensions;
using App.Application.Utility;
using App.Domain.GameWorld;
using Microsoft.FSharp.Core;
using HillModule = App.Domain.GameWorld.HillModule;

namespace App.Application.Policy.GameHillSelector;

public class RandomHill(IRandom random, IHills hills, List<string>? excludeFormattedStrings, IMyLogger logger)
    : IGameHillSelector
{
    public async Task<Guid> Select(CancellationToken ct)
    {
        var allHills = (await hills.GetAll(ct)).ToList();
        if (allHills.Count == 0)
        {
            throw new Exception("No hill to select from");
        }

        var hillsToExclude = excludeFormattedStrings ?? new List<string>();
        var formattedHillsToExclude = hillsToExclude
            .Select(SearchFormattedNameModule.tryCreate)
            .Where(opt => opt.IsSome())
            .Select(opt => SearchFormattedNameModule.value(opt.Value))
            .ToHashSet();

        var filteredHills = allHills
            .Select(hill => new { hill, formatted = CreateFormatted(hill.Location, hill.HsPoint) })
            .Where(x => x.formatted == null || !formattedHillsToExclude.Contains(x.formatted))
            .Select(x => x.hill).ToList();

        if (filteredHills.Count == 0)
        {
            throw new Exception("No hill to select from");
        }

        return filteredHills.GetRandomElement(random).Id.Item;

        string? CreateFormatted(HillModule.Location location, HillModule.HsPoint hs)
        {
            var opt = SearchFormattedNameModule.tryCreate($"{location.Item} HS{
                HillModule.HsPointModule.value(hs)}");
            var maybeFormattedName = FSharpOption<SearchFormattedName>.get_IsSome(opt)
                ? SearchFormattedNameModule.value(opt.Value)
                : null;
            if (maybeFormattedName is null)
            {
                logger.Warn($"Hill ({location.Item}) has no formatted name");
            }

            return maybeFormattedName;
        }
    }
}