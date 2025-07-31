using App.Application.Commanding;
using App.Domain.Game;

namespace App.Application.UseCase.Helper;

public interface IQuickGameMatchmakingSettingsProvider : IAsyncValueProvider<Domain.Matchmaking.Settings>;

public interface IQuickGameServerProvider : IAsyncValueProvider<ServerModule.Id>;

public interface IQuickGameSettingsProvider : IAsyncValueProvider<Domain.Game.Settings.Settings>;