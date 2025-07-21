using App.Application.Abstractions;
using App.Application.ReadModel.Projection;
using Random = App.Domain.Shared.Random;

namespace App.Application.UseCase.Game.ChooseQuickGameHill;

public record Command(
) : ICommand<Domain.GameWorld.HillId>;

public class Handler(
    IGameWorldHillProjection gameWorldHills,
    Random.IRandom random
) : ICommandHandler<Command, Domain.GameWorld.HillId>
{
    public async Task<Domain.GameWorld.HillId> HandleAsync(Command command, CancellationToken ct)
    {
        var hills = (await gameWorldHills.GetAllAsync()).ToArray();
        var randomIndex = random.RandomInt(0, hills.Length - 1);
        var hill = hills[randomIndex];
        return Domain.GameWorld.HillId.NewHillId(hill.Id);
    }
}