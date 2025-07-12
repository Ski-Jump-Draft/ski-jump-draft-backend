using App.Application.Abstractions;
using App.Application.UseCase.Competition.Engine.Factory;
using App.Domain.Competition;
using App.Domain.Shared;
using App.Plugin.Engine.Classic;

namespace App.Plugin.Plugins.Classic;

public class Plugin(
    Dictionary<Hill, double> gatePoints,
    Dictionary<Hill, double> headwindPoints,
    Dictionary<Hill, double> tailwindPoints,
    IGuid guid
) : ICompetitionEnginePlugin
{
    public string PluginId => "classic";

    public Domain.Competition.Engine.ITemplate Template => new Template();

    public ICompetitionEngineFactory Factory => new Factory(gatePoints, headwindPoints, tailwindPoints, guid);
}