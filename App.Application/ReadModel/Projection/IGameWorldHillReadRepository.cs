namespace App.Application.ReadModel.Projection;

public interface IGameWorldHillProjection
{
    Task<IEnumerable<GameWorldHillDto>> GetAllAsync();
}

public record GameWorldHillDto(Guid Id, string Location, string CountryCode, double KPoint, double HsPoint);