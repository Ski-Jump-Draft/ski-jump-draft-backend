namespace App.Application.Abstractions.Mappers;

public interface IValueMapper<in TInput, out TOutput>
{
    TOutput Map(TInput id);
}