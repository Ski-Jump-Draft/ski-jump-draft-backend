using App.Application.JumpersForm;
using App.Application.Utility;

namespace App.Application.Policy.GameFormAlgorithm;

public class FullyRandom(IRandom random) : IJumperGameFormAlgorithm
{
    public double CalculateFromLiveForm(double _)
    {
        var probabilities = new Dictionary<int, int>()
        {
            { 1, 1 },
            { 2, 2 },
            { 3, 4 },
            { 4, 8 },
            { 5, 10 },
            { 6, 10 },
            { 7, 8 },
            { 8, 4 },
            { 9, 2 },
            { 10, 1 },
        };
        return probabilities[random.RandomInt(1, 11)];
    }
}