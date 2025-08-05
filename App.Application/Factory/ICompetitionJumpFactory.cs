namespace App.Application.Factory;

public interface ICompetitionJumpFactory
{
    Domain.SimpleCompetition.Jump Create(Domain.Simulating.Jump simulatedJump);
}