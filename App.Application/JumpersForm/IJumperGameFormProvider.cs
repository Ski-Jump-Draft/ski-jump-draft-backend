namespace App.Application.JumpersForm;

public interface IJumperGameFormStorage
{
    // double GetGameForm(Guid gameId, Guid gameWorldJumperId);
    // double Add(Guid gameId, Guid gameWorldJumperId, double form);
    double GetGameForm(Guid gameJumperId);
    double Add(Guid gameJumperId, double form);
    double Remove(Guid gameJumperId);
}