using App.Application.Commanding;
using App.Domain.Game;

namespace App.Application.UseCase.Helper;

public interface IQuickGameMatchmakingSettingsProvider : IAsyncValueProvider<Domain.Matchmaking.Settings>;

public interface IQuickGameServerProvider : IAsyncValueProvider<ServerModule.Id>;

public interface IQuickGameSettingsProvider : IAsyncValueProvider<Domain.Game.Settings.Settings>;

public interface IQuickGamePreDraftSettingsProvider : IAsyncValueProvider<Domain.PreDraft.Settings.Settings>;

public interface IQuickGameDraftSettingsProvider : IAsyncValueProvider<Domain.Draft.Settings.Settings>;

public record QuickGameCompetitionSettings(
    Domain.SimpleCompetition.CompetitionModule.Type Type,
    Domain.SimpleCompetition.Settings Settings,
    Domain.SimpleCompetition.Hill Hill);

public interface IQuickGameCompetitionSettingsProvider : IAsyncValueProvider<QuickGameCompetitionSettings>;