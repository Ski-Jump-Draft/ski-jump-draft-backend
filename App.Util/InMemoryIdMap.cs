using System.Collections.Concurrent;

namespace App.Util;

public class InMemoryBiDirectionalIdMap<TIdA, TIdB> : IBiDirectionalIdMap<TIdA, TIdB>
    where TIdA : notnull
    where TIdB : notnull
{
    private readonly ConcurrentDictionary<TIdA, TIdB> _forward = new();
    private readonly ConcurrentDictionary<TIdB, TIdA> _reverse = new();

    public void Add(TIdA a, TIdB b)
    {
        if (!_forward.TryAdd(a, b))
            throw new ArgumentException($"ID A '{a}' already mapped");

        if (_reverse.TryAdd(b, a)) return;

        _forward.TryRemove(a, out _);
        throw new ArgumentException($"ID B '{b}' already mapped");
    }

    public TIdB Map(TIdA a)
    {
        if (_forward.TryGetValue(a, out var b))
            return b;
        throw new KeyNotFoundException($"No mapping found for A '{a}'");
    }

    public TIdA MapBackward(TIdB b)
    {
        if (_reverse.TryGetValue(b, out var a))
            return a;
        throw new KeyNotFoundException($"No mapping found for B '{b}'");
    }

    public bool TryMap(TIdA a, out TIdB b)
        => _forward.TryGetValue(a, out b!);

    public bool TryMapBackward(TIdB b, out TIdA a)
        => _reverse.TryGetValue(b, out a!);
}