namespace App.Application.Commanding;

public interface IAsyncValueProvider<T>
{
    Task<T> Provide();
}

public interface IValueProvider<out T>
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