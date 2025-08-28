namespace App.Application._2.Policy.GameJumpersSelector;

public class Mocked : IGameJumpersSelector
{
    public Task<IEnumerable<SelectedJumperDto>> Select()
    {
        IEnumerable<SelectedJumperDto> jumpers = new List<SelectedJumperDto>
        {
            new SelectedJumperDto(Guid.Parse("2379a780-a5b9-424b-9367-d28ccb2127e3"), "POL", "Jan", "Kowalski"),
            new SelectedJumperDto(Guid.Parse("da748870-ad85-4917-9ec4-81e77fac6bd6"), "POL", "Zygmunt", "Młody"),
            new SelectedJumperDto(Guid.Parse("5e3ed57c-62d5-4a40-a2ba-f70b165c6f3f"), "POL", "Michał", "Karczewski"),
            new SelectedJumperDto(Guid.Parse("5e3ed57c-62d5-4a40-a2ba-f70b165c6f3f"), "POL", "Maciej", "Pies"),
            new SelectedJumperDto(Guid.Parse("5e3ed57c-62d5-4a40-a2ba-f70b165c6f3f"), "POL", "Piotr", "Tętnica"),
            new SelectedJumperDto(Guid.Parse("5e3ed57c-62d5-4a40-a2ba-f70b165c6f3f"), "POL", "Kamil", "Staszek"),
        };
        return Task.FromResult(jumpers);
    }
}