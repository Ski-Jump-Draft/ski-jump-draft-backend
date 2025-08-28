namespace App.Application._2.Policy.GameJumpersSelector;

public interface IGameJumpersSelector
{
    Task<IEnumerable<SelectedJumperDto>> Select();
}

public record SelectedJumperDto(
    Guid Id,
    string Alpha3,
    string Name,
    string Surname
);