namespace App.Application.ReadModel.Projection;

public interface IGameWorldCountryQuery
{
    Task<GameWorldCountryCodeDto?> GetCountryCodeByIdAsync(Domain.GameWorld.CountryModule.Id countryId);
}

public record GameWorldCountryCodeDto(
    Guid CountryId,
    string CountryCode
);