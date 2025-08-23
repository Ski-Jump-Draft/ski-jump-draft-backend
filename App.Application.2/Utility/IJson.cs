namespace App.Application._2.Utility;

public interface IJson
{
    string Serialize<T>(T value);
    T Deserialize<T>(string json);
}
