namespace App.Application.UseCase.Helper;

public interface IQuickGameHillSelector
{
    Domain.GameWorld.Hill Select();
}