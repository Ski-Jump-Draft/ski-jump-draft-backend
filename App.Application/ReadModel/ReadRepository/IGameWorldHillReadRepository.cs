namespace App.Application.ReadModel.ReadRepository;

public interface IGameWorldHillReadRepository
{
    Task<IEnumerable<GameWorldHillDto>> GetAllAsync();
}

public record GameWorldHillDto(Guid Id, string Location, string CountryCode, double KPoint, double HsPoint);