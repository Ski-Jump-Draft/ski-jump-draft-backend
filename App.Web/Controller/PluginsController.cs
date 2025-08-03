using App.Application.Commanding;
using App.Application.CompetitionEngine;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Controller;

public class PluginsController : ControllerBase
{
    [HttpGet("plugins")]
    public async Task<IActionResult> Plugins([FromServices] ICompetitionEnginePluginRepository repo)
    {
        var plugin = await repo.GetByIdAsync("classic");
        return Ok(plugin?.PluginId);
    }
}