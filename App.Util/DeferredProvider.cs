namespace App.Util;

public class DeferredProvider<T>
{
    private Func<T> _provider = () => throw new InvalidOperationException($"Provider for {typeof(T).Name} not yet assigned");

    public void Set(Func<T> provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    public T Provide() => _provider();
}