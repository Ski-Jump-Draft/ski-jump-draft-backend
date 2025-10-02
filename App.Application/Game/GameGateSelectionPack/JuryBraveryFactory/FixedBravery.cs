using App.Application.Policy.GameGateSelector;

namespace App.Application.Game.GameGateSelectionPack.JuryBraveryFactory;

public class FixedBravery(JuryBravery bravery) : IJuryBraveryFactory
{
    public JuryBravery Create() => bravery;
}