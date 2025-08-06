namespace App.Application.Commanding.Mappers;

public interface IValueMapper<in TInput, out TOutput>
{
    TOutput Map(TInput id);
}