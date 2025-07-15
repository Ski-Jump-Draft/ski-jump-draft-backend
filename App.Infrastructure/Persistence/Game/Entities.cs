namespace App.Infrastructure.Persistence.Game;

public class GameEntity
{
    public System.Guid Id { get; set; }
    public string Phase { get; set; }
    public DateTimeOffset Created { get; set; }
    public int CurrentPlayers { get; set; }
    public int MaxPlayers { get; set; }
}

public class ParticipantEntity
{
    public System.Guid Id { get; set; }
    public System.Guid GameId { get; set; }
    public string Nick { get; set; }
}