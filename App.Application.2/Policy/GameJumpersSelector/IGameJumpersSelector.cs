namespace App.Application._2.Policy.GameJumpersSelector;

public interface IGameJumpersSelector
{
    Task<IEnumerable<SelectedGameWorldJumperDto>> Select(CancellationToken ct);
}

public record SelectedGameWorldJumperDto(
    Guid Id,
    string CountryFisCode,
    string Name,
    string Surname
);