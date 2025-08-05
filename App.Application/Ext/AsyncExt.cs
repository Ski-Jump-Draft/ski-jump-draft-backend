namespace App.Application.Ext;

public static class AsyncExt
{
    public static async Task AwaitOrWrap(this Task task, Func<System.Exception, System.Exception> wrap)
    {
        try
        {
            await task;
        }
        catch (System.Exception ex)
        {
            var wrapped = wrap(ex);
            if (wrapped.InnerException == null && wrapped is not null)
                throw new System.Exception(wrapped.Message, ex);
            throw wrapped!;
        }
    }

    public static async Task<T> AwaitOrWrap<T>(this Task<T> task, Func<System.Exception, System.Exception> wrap)
    {
        try
        {
            return await task;
        }
        catch (System.Exception ex)
        {
            var wrapped = wrap(ex);
            if (wrapped.InnerException == null && wrapped is not null)
                throw new System.Exception(wrapped.Message, ex);
            throw wrapped!;
        }
    }
    
    public static async Task<T> AwaitOrWrapNullable<T>(
        this Task<T?> task,
        Func<System.Exception, System.Exception> wrap
    ) where T : class
    {
        try
        {
            var result = await task.ConfigureAwait(false);
            if (result is null)
                throw new InvalidOperationException("Task returned null for non-nullable AwaitOrWrap.");

            return result;
        }
        catch (System.Exception ex)
        {
            var wrapped = wrap(ex);
            if (wrapped.InnerException == null && wrapped is not null)
                throw new System.Exception(wrapped.Message, ex);
            throw wrapped!;
        }
    }

}