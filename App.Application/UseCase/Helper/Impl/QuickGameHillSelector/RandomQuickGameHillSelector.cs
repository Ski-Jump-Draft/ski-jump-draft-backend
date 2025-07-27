using App.Domain.GameWorld;

namespace App.Application.UseCase.Helper.Impl.QuickGameHillSelector;

public sealed class RandomQuickGameHillSelector(IEnumerable<Hill> hills) : IQuickGameHillSelector
{
    public Hill Select()
    {
        return hills.ElementAt(new Random().Next(hills.Count()));
    }
}