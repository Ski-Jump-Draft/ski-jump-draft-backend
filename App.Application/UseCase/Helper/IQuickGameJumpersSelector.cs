using App.Domain.GameWorld;

namespace App.Application.UseCase.Helper;

public interface IQuickGameJumpersSelector
{
    Task<IEnumerable<JumperTypes.Id>> Select();
}