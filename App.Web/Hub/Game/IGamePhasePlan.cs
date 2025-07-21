namespace App.Web.Hub.Game;

public interface IGamePhasePlan
{
    PhaseInfo GetNextPhase(Guid gameId);
}

public record PhaseInfo(string Code, DateTimeOffset ScheduledAt);
