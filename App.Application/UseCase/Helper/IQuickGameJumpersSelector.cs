namespace App.Application.UseCase.Helper;

public interface IQuickGameJumpersSelector
{
    IEnumerable<Domain.GameWorld.Jumper> Select();
}