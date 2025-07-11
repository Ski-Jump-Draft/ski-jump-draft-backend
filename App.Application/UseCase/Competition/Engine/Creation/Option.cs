namespace App.Application.UseCase.Competition.Engine.Creation;

public enum OptionType
{
    String,
    Double,
    Boolean,
    ListOfIntegers,
    ListOfStrings
}

public sealed record Option(string Key, OptionType Type);