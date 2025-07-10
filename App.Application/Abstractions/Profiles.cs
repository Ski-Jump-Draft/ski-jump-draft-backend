using App.Domain.Profile;

namespace App.Application.Abstractions;

public interface IUserTranslator<TOutput>
{
    Task<TOutput> CreateTranslatedAsync(User.Id id);
}