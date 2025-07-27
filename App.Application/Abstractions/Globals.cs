using App.Domain.Game;

namespace App.Application.Abstractions;

public interface IQuickGameServerProvider : IAsyncValueProvider<ServerModule.Id>;

public interface IQuickGameSettingsProvider : IAsyncValueProvider<Domain.Game.Settings.Settings>;