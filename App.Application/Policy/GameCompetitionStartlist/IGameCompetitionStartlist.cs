namespace App.Application.Policy.GameCompetitionStartlist;

public interface IGameCompetitionStartlist
{
    Task<IReadOnlyList<Domain.Game.JumperId>> Get(Guid gameId, GameCompetitionDto competition, CancellationToken ct);
}

public abstract record GameCompetitionDto;

public sealed record PreDraftDto(int CompetitionIndex) : GameCompetitionDto;

public sealed record MainCompetitionDto() : GameCompetitionDto;