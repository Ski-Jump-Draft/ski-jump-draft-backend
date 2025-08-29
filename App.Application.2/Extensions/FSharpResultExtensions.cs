using Microsoft.FSharp.Core;
using System.Reflection;
using Microsoft.FSharp.Reflection;

namespace App.Application._2.Extensions;

public static class FSharpResultExtensions
{
    // zwraca nazwę przypadku ("Ok" lub "Error") i pola
    private static (string CaseName, object[] Fields) GetUnionCase<TSuccess, TError>(
        FSharpResult<TSuccess, TError> result)
    {
        var boxed = (object)result; // box value/union
        var union = FSharpValue.GetUnionFields(boxed, typeof(FSharpResult<TSuccess, TError>),
            FSharpOption<BindingFlags>.None);
        var caseInfo = union.Item1;
        var fields = union.Item2 ?? [];
        return (caseInfo.Name, fields);
    }

    public static TSuccess OrThrow<TSuccess, TError>(this FSharpResult<TSuccess, TError> result,
        Func<TError, Exception>? errorFactory = null)
    {
        var (caseName, fields) = GetUnionCase<TSuccess, TError>(result);
        if (caseName == "Ok")
            return (TSuccess)fields[0]!;
        var err = (TError)fields[0]!;
        throw errorFactory?.Invoke(err) ?? new InvalidOperationException($"Result was Error: {err}");
    }
    
    public static TSuccess OrThrow<TSuccess, TError>(this FSharpResult<TSuccess, TError> result,
        object exception)
    {
        var (caseName, fields) = GetUnionCase<TSuccess, TError>(result);
        if (caseName == "Ok")
            return (TSuccess)fields[0]!;
        var err = (TError)fields[0]!;
        throw new Exception(exception.ToString());
    }
}