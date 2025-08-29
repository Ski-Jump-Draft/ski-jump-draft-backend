using System.Text.Json;
using App.Application._2.Utility;

namespace App.Infrastructure._2.Utility.Json;

public class DefaultJson : IJson
{
    public string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value);
    }

    public T Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json)!;
    }
}