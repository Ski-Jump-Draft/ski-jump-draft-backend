namespace App.Application.Abstractions;

public interface IAdvancementTieBreaker
{
    /// Zwraca podzbiór `tied` (może być większy niż 1 gdy dopuszczasz wielu ex-aequo)
    IEnumerable<Guid> BreakTies(
        IEnumerable<Guid> tied);
}