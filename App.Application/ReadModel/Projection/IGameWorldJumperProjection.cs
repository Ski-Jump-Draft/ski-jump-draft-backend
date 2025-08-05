namespace App.Application.ReadModel.Projection;

public interface IGameWorldJumperProjection
{
    Task<IEnumerable<GameWorldJumperDto>> GetByIds(IEnumerable<Domain.GameWorld.JumperTypes.Id> gameWorldJumperIds);
    Task<IEnumerable<GameWorldJumperDto>> GetAllAsync();
}

public record GameWorldJumperDto(
    Guid Id,
    string Name,
    string Surname,
    string CountryId,
    double Takeoff,
    double Flight,
    double Landing,
    double LiveForm);