using System.Globalization;
using App.Application.ReadModel.Projection;
using CsvHelper;

namespace App.Infrastructure.DataSourceHelpers;

public class GameWorldJumpersLoader(string path)
{
    public async Task<List<GameWorldJumperDto>> LoadAllAsync(CancellationToken ct)
    {
        await using var stream = File.OpenRead(path);
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        return csv.GetRecords<GameWorldJumperDto>().ToList();
    }
}