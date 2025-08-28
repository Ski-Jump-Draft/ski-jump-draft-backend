namespace App.Application._2.Policy.GameHillSelector;

public class Mocked() : IGameHillSelector
{
    public Task<SelectedHillDto> Select()
    {
        return Task.FromResult(new SelectedHillDto(Guid.Parse("9053514d-989d-4ddd-b8a0-9f0b3634cc04"), "Wielka Krokiew", "Zakopane", 125, 140, 7, 10.8, 17.2));
    }
}