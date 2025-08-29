namespace App.Infrastructure._2.Helper.Csv.GameWorldCountryIdProvider;

public interface IGameWorldCountryIdProvider
{
    Task<Guid> GetFromFisCode(string fisCode, CancellationToken ct);
}