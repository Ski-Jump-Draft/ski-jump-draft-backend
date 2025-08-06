using App.Domain.Profile;

namespace App.Application.Commanding;

public interface IUserTranslator<TOutput>
{
    Task<TOutput> CreateTranslatedAsync(User.Id id);
}