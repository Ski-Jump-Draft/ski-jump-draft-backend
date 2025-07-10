namespace App.Plugin.Engine.Classic;

public class Template : Domain.Competition.Engine.ITemplate
{
    public Domain.Competition.Engine.Template.Name Name =>
        Domain.Competition.Engine.Template.Name.NewName("Klasyczny silnik konkursowy");

    public Domain.Competition.Engine.Template.Description Description =>
        Domain.Competition.Engine.Template.Description.NewDescription(
            "Silnik umożliwiający rozegranie wszystkich formatów znanych ze świata realnych skoków narciarskich. Nadaje się do konkursów z prostymi zasadami awansu (np. zmniejszające się co rundę limity), jak klasyczny konkurs PŚ, finał Raw Air, drużynówki, duety a także Turniej Czterech Skoczni. Pozwala na pewną dozę eksperymentownaia, ale nie nada się do bardzo nietypowych pomysłów.");

    public Domain.Competition.Engine.Template.Author Author =>
        Domain.Competition.Engine.Template.Author.NewAuthor("Konrad Król");
}