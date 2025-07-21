using App.Application.Abstractions;
using App.Application.Exception;
using App.Application.Ext;
using App.Application.UseCase.Game.Exception;
using App.Domain.Matchmaking;
using App.Domain.Shared;
using App.Domain.Repositories;
using Microsoft.FSharp.Control;
using Random = App.Domain.Shared.Random;

namespace App.Application.UseCase.Game.ChooseQuickGameHill;

public record Command(
) : ICommand<App.Domain.GameWorld.HillModule.Id>;

public class Handler(
    IGameWorldHillRepository gameWorldHills,
    Random.IRandom random
) : ICommandHandler<Command, App.Domain.GameWorld.HillModule.Id>
{
    public async Task<App.Domain.GameWorld.HillModule.Id> HandleAsync(Command command, CancellationToken ct)
    {
        var hills = await FSharpAsyncExt.Await(gameWorldHills.GetAllAsync(), ct);
        var randomIndex = random.RandomInt(0, hills.Length - 1);
        var hill = hills[randomIndex];
        return hill.Id;
    }
}