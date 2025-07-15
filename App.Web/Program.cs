using System.Text.Json;
using App.Web;
using App.Web.Controller;
using App.Web.Hub;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services
    //.AddApplication()
   // .AddEventSourcingRepositories(builder.Configuration)
    .AddCrudRepositories(builder.Configuration);
builder.Services.AddCors(o => o.AddDefaultPolicy(p => 
    p.WithOrigins("http://localhost:3000").AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", opts =>
    {
        opts.Authority = "http://localhost:5001";
        opts.Audience  = "game‑api";
        opts.RequireHttpsMetadata = false;
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapPost("/game/join", ([FromBody] JoinDto dto) =>
        Results.Ok(new 
        {
            gameId        = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            participantId = Guid.Parse("22222222-2222-2222-2222-222222222222")
        })
    );

    app.MapPost("/game/leave", ([FromBody] QuitDto dto) =>
        Results.NoContent()
    );

    app.MapGet("/game/matchmaking", async (HttpContext ctx, CancellationToken ct) =>
    {
        ctx.Response.ContentType = "text/event-stream";

        var events = new MatchmakingEvent[]
        {
            new("updated", new { CurrentPlayersCount = 1, MaxPlayersCount = 6 }),
            new("updated", new { CurrentPlayersCount = 2, MaxPlayersCount = 6 }),
            new("updated", new { CurrentPlayersCount = 3, MaxPlayersCount = 6 }),
            new("updated", new { CurrentPlayersCount = 4, MaxPlayersCount = 6 }),
            new("updated", new { CurrentPlayersCount = 3, MaxPlayersCount = 6 }),
            new("failed", new {PlayersCount = 3, MaxPlayersCount = 6 }),
            // new("updated", new { CurrentPlayersCount = 4, MaxPlayersCount = 6 }),
            // new("ended",   new { PlayersCount = 4 })
        };

        foreach (var ev in events)
        {
            await ctx.Response.WriteAsync($"event: {ev.Type}\n", ct);
            await ctx.Response.WriteAsync($"data: {JsonSerializer.Serialize(ev.Data)}\n\n", ct);
            await ctx.Response.Body.FlushAsync(ct);
            await Task.Delay(2000, ct);
        }
    });
}
else
{
    app.MapControllers();
    app.MapHub<MatchmakingHub>("/matchmakingHub");
}

app.Run();
