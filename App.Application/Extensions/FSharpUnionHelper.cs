using Microsoft.FSharp.Reflection;

namespace App.Application.Extensions;

public static class FSharpUnionHelper
{
    public static string GetCaseName(object unionValue)
    {
        var unionType = unionValue.GetType();
        var cases = FSharpType.GetUnionCases(unionType, null);
        var tag = (int)unionType.GetProperty("Tag")!.GetValue(unionValue)!;
        
        return cases[tag].Name;
    }
}