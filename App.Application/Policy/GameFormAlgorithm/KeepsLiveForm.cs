using App.Application.JumpersForm;

namespace App.Application.Policy.GameFormAlgorithm;

public class KeepsLiveForm : IJumperGameFormAlgorithm
{
    public double CalculateFromLiveForm(double liveForm)
    {
        return liveForm;
    }
}