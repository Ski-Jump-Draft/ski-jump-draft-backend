using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;

namespace App.Application.Ext;

public static class FSharpAsyncExt
{
    public static async Task<T> Await<T>(FSharpAsync<T> fasync, CancellationToken ct)
    {
        var res = await FSharpAsync.StartAsTask(fasync, null, null);
        return res;
    }

    public static async Task<T> AwaitOrThrow<T>(FSharpAsync<T> fasync, string msg, CancellationToken ct)
    {
        var res = await FSharpAsync.StartAsTask(fasync, null, null);
        if (FSharpOption<T>.get_IsNone(res))
            throw new InvalidOperationException(msg);
        return res;
    }

    public static async Task<T> AwaitOrThrow<T>(FSharpAsync<T> fasync, System.Exception error, CancellationToken ct)
    {
        var res = await FSharpAsync.StartAsTask(fasync, null, null);
        if (FSharpOption<T>.get_IsNone(res))
            throw error;
        return res;
    }

    public static async Task<T> AwaitOrThrow<T>(FSharpAsync<FSharpOption<T>> fasync, CancellationToken ct, string msg)
    {
        var res = await FSharpAsync.StartAsTask(fasync, null, null);
        if (FSharpOption<T>.get_IsNone(res))
            throw new InvalidOperationException(msg);
        return res.Value;
    }

    public static async Task<T> AwaitOrThrow<T>(FSharpAsync<FSharpOption<T>> fasync, System.Exception error,
        CancellationToken ct)
    {
        var res = await FSharpAsync.StartAsTask(fasync, null, null);
        if (FSharpOption<T>.get_IsNone(res))
            throw error;
        return res.Value;
    }

    public static async Task AwaitOrThrow(FSharpAsync<Unit> fasync, System.Exception error, CancellationToken ct)
    {
        try
        {
            await FSharpAsync.StartAsTask(fasync, null, ct);
        }
        catch
        {
            throw error;
        }
    }

    public static FSharpAsync<T> Return<T>(T value) =>
        FSharpAsync.AwaitTask(Task.FromResult(value));
}