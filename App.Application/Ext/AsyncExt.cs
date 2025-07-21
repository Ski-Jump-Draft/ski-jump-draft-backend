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
            throw wrap(ex);
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
            throw wrap(ex);
        }
    }
}