using App.Application.Extensions;
using App.Application.Policy.GameGateSelector;
using App.Application.Utility;

namespace App.Application.Game.GameGateSelectionPack.JuryBraveryFactory;

public class RandomBravery(IRandom rng) : IJuryBraveryFactory
{
    public JuryBravery Create()
    {
        var probabilityByJuryBravery = new Dictionary<JuryBravery, double>
        {
            { JuryBravery.VeryLow, 1 },
            { JuryBravery.Low, 6 },
            { JuryBravery.Medium, 6 },
            { JuryBravery.High, 3 },
            { JuryBravery.VeryHigh, 0.5 }
        };
        return probabilityByJuryBravery.WeightedRandomElement(rng);
    }
}