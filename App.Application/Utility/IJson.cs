namespace App.Application.Utility;

public interface IJson
{
    string Serialize<T>(T value);
    T Deserialize<T>(string json);
}
