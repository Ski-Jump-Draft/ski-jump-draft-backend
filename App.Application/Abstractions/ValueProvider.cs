namespace App.Application.Abstractions;

public interface IValueProvider<T>
{
    T Provide();
}

public class JustValueProvider<T>(T value) : IValueProvider<T>
{
    public T Provide()
    {
        return value;
    }
}