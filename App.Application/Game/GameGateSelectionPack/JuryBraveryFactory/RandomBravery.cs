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
            { JuryBravery.VeryLow, 4 },
            { JuryBravery.Low, 10 },
            { JuryBravery.Medium, 6 },
            { JuryBravery.High, 1 },
            { JuryBravery.VeryHigh, 0.2 }
        };
        return probabilityByJuryBravery.WeightedRandomElement(rng);
    }
}