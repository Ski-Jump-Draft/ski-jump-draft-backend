using App.Application.JumpersForm;
using App.Application.Utility;

namespace App.Application.Policy.GameFormAlgorithm;

public class SlightlyRandomized(IRandom random) : IJumperGameFormAlgorithm
{
    public double CalculateFromLiveForm(double liveForm)
    {
        return liveForm + random.Gaussian(0, 0.5);
    }
}