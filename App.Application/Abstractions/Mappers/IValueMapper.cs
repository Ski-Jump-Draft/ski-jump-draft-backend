namespace App.Application.Abstractions.Mappers;

public interface IValueMapper<out TResult, in TId>
{
    TResult Map(TId id);
}