namespace App.Application.JumpersForm;

public interface IJumperLiveFormProvider
{
    double GetJumperLiveForm(Guid gameWorldJumperId);
}