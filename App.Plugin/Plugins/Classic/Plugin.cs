using App.Application.Commanding;
using App.Application.Factory;
using App.Domain.Shared;
using App.Plugin.Engine.Classic;

namespace App.Plugin.Plugins.Classic;

public class Plugin(
    App.Plugin.Engine.Classic.Factory factory
    // Dictionary<Hill, double> gatePoints,
    // Dictionary<Hill, double> headwindPoints,
    // Dictionary<Hill, double> tailwindPoints,
) : ICompetitionEnginePlugin
{
    public string PluginId => "classic";

    public Domain.Competition.Engine.Metadata Metadata => new Metadata();

    public ICompetitionEngineFactory Factory =>
        factory; // new Factory(gatePoints, headwindPoints, tailwindPoints, guid);
}