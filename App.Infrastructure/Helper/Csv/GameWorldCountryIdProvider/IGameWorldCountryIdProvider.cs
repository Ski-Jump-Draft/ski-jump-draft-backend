namespace App.Infrastructure.Helper.Csv.GameWorldCountryIdProvider;

public interface IGameWorldCountryIdProvider
{
    Task<Guid> GetFromFisCode(string fisCode, CancellationToken ct);
}