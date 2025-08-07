using App.Application.ReadModel.Projection;
using App.Domain.GameWorld;

namespace App.Application.UseCase.Helper.Impl.QuickGameJumpersSelector;

public class AllJumpers(IGameWorldJumperQuery gameWorldJumperQuery) : IQuickGameJumpersSelector
{
    public async Task<IEnumerable<JumperTypes.Id>> Select()
    {
        var gameWorldJumpers = await gameWorldJumperQuery.GetAllAsync();
        return gameWorldJumpers.Select(jumper => JumperTypes.Id.NewId(jumper.Id));
    }
}