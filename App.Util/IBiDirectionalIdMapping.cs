namespace App.Util;

public interface IBiDirectionalIdMap<TIdA, TIdB>
{
    void Add(TIdA a, TIdB b);
    TIdB Map(TIdA a);
    TIdA MapBackward(TIdB b);
    bool TryMap(TIdA a, out TIdB b);
    bool TryMapBackward(TIdB b, out TIdA a);
}