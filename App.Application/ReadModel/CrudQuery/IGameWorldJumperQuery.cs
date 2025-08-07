namespace App.Application.ReadModel.Projection;

public interface IGameWorldJumperQuery
{
    Task<IEnumerable<GameWorldJumperDto>> GetByIds(IEnumerable<Domain.GameWorld.JumperTypes.Id> gameWorldJumperIds,
        CancellationToken ct = default);

    Task<IEnumerable<GameWorldJumperDto>> GetAllAsync(CancellationToken ct = default);
}

public record GameWorldJumperDto(
    Guid Id,
    string Name,
    string Surname,
    Guid CountryId,
    string CountryCode,
    int Takeoff,
    int Flight,
    int Landing,
    int LiveForm);