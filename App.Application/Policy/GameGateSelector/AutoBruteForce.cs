using App.Application.Acl;
using App.Application.Game.Gate;

namespace App.Application.Policy.GameGateSelector;

/// <summary>
/// Select an optimal gate by checking whether any of the jumpers have reached the HS Point on in a given gate
/// </summary>
public class AutoBruteForce(IGameJumperAcl gameJumperAcl) : IGameGateSelector
{
    public int Select(Domain.Game.Game game)
    {
        
    }
}