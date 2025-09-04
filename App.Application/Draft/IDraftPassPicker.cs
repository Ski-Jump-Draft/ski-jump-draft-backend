namespace App.Application.Draft;

public interface IDraftPassPicker
{
    Domain.Game.JumperId Pick(Domain.Game.Game game);
}