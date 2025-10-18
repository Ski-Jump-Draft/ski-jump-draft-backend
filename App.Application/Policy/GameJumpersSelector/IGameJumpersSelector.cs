namespace App.Application.Policy.GameJumpersSelector;

public interface IGameJumpersSelector
{
    Task<IEnumerable<SelectedGameWorldJumperDto>> Select(CancellationToken ct);
}

public record SelectedGameWorldJumperDto(
    Guid GameWorldJumperId,
    string CountryFisCode,
    string Name,
    string Surname,
    double LiveForm
);