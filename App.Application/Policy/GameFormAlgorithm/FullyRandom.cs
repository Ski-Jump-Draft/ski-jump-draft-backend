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
            { 3, 5 },
            { 4, 15 },
            { 5, 20 },
            { 6, 20 },
            { 7, 15 },
            { 8, 5 },
            { 9, 2 },
            { 10, 1 },
        };

        var total = probabilities.Values.Sum();
        var r = random.RandomInt(0, total);
        var cum = 0;

        foreach (var kv in probabilities)
        {
            cum += kv.Value;
            if (r < cum)
                return kv.Key;
        }

        return probabilities.Keys.Last();
    }
}