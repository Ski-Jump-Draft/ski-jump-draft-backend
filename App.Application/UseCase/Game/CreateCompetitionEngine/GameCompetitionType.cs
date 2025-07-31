namespace App.Application.UseCase.Game.CreateCompetitionEngine;

public abstract record GameCompetitionType
{
    public sealed record PostDraftCompetition() : GameCompetitionType;

    public sealed record PreDraft(int Index) : GameCompetitionType;
}