namespace App.Application.ReadModel.CrudQuery;

public interface IGameWorldCountryQuery
{
    Task<GameWorldCountryCodeDto?> GetCountryCodeByIdAsync(Domain.GameWorld.CountryModule.Id countryId);
}

public record GameWorldCountryCodeDto(
    Guid CountryId,
    string CountryCode
);