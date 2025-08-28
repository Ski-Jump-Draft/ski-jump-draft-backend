namespace App.Application._2.Policy.GameHillSelector;

public interface IGameHillSelector
{
    Task<SelectedHillDto> Select();
}

public record SelectedHillDto(
    Guid Id,
    string Name,
    string Location,
    int KPoint,
    int HsPoint,
    double GatePoints,
    double HeadwindPoints,
    double TailwindPoints
);