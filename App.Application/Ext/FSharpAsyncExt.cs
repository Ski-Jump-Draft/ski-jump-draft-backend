using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;

namespace App.Application.Ext;

public static class FSharpAsyncExt
{
    public static async Task<T> AwaitOrThrow<T>(FSharpAsync<T> fasync, CancellationToken ct, string? msg = null)
    {
        var res = await FSharpAsync.StartAsTask(fasync, null, null);
        if (FSharpOption<T>.get_IsNone(res))
            throw new InvalidOperationException(msg ?? "Not found");
        return res;
    }

    public static async Task<T> AwaitOrThrow<T>(FSharpAsync<T> fasync, CancellationToken ct, System.Exception error)
    {
        var res = await FSharpAsync.StartAsTask(fasync, null, null);
        if (FSharpOption<T>.get_IsNone(res))
            throw error;
        return res;
    }
    
    public static async Task<T> AwaitOrThrow<T>(FSharpAsync<FSharpOption<T>> fasync, CancellationToken ct, string? msg = null)
    {
        var res = await FSharpAsync.StartAsTask(fasync, null, null);
        if (FSharpOption<T>.get_IsNone(res))
            throw new InvalidOperationException(msg ?? "Not found");
        return res.Value;
    }
    
    public static async Task<T> AwaitOrThrow<T>(FSharpAsync<FSharpOption<T>> fasync, CancellationToken ct, System.Exception error)
    {
        var res = await FSharpAsync.StartAsTask(fasync, null, null);
        if (FSharpOption<T>.get_IsNone(res))
            throw error;
        return res.Value;
    }
}
