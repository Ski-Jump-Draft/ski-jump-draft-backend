namespace App.Domain.GameWorld


type Jumper =
    { Id: JumperTypes.Id
      Name: JumperTypes.Name
      Surname: JumperTypes.Surname
      CountryId: Country.Id
      Skills: JumperSkills }
