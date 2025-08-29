namespace App.Application._2.Extensions;

using Microsoft.FSharp.Core;

public static class FSharpOptionExtensions
{
    public static bool IsSome<T>(this FSharpOption<T> opt) =>
        OptionModule.IsSome(opt);

    public static bool IsNone<T>(this FSharpOption<T> opt) =>
        OptionModule.IsNone(opt);

    public static T GetValueOrDefault<T>(this FSharpOption<T> opt, T defaultValue = default!) =>
        OptionModule.IsSome(opt) ? opt.Value : defaultValue;

    public static T OrThrow<T>(this FSharpOption<T> opt, object exception) =>
        opt != null ? opt.Value : throw new Exception(exception.ToString());
}