namespace App.Application.UseCase.Helper;

public interface IDraftSubjectsFactory
{
    IEnumerable<Domain.Draft.Subject.Subject> CreateIndividuals(
        IEnumerable<Domain.GameWorld.Jumper> gameWorldJumpers);
}