using App.Application.Commanding;
using App.Application.ReadModel.Projection;
using Random = App.Domain.Shared.Random;

namespace App.Application.UseCase.Game.QuickGame.ChooseHill;

public record Command(
) : ICommand<Domain.GameWorld.HillTypes.Id>;

public class Handler(
    IGameWorldHillProjection gameWorldHills,
    Random.IRandom random
) : ICommandHandler<Command, Domain.GameWorld.HillTypes.Id>
{
    public async Task<Domain.GameWorld.HillTypes.Id> HandleAsync(Command command, MessageContext messageContext,
        CancellationToken ct)
    {
        var hills = (await gameWorldHills.GetAllAsync()).ToArray();
        var randomIndex = random.RandomInt(0, hills.Length - 1);
        var hill = hills[randomIndex];
        return Domain.GameWorld.HillTypes.Id.NewId(hill.Id);
    }
}