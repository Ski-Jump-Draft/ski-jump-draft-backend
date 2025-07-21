using App.Web.DependencyInjection;
using App.Web.Hub.Game;
using App.Web.Hub.Matchmaking;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();
// TODO: Na produkcji
//
// builder.Services.AddMongo(builder.Configuration);
// builder.Services.AddRedis(builder.Configuration);
builder.Services
    .AddCrudRepositories(builder.Configuration)
    .AddProjections()
    .AddReadRepositories(builder.Configuration)
    .AddEventsInfrastructure()
    .AddCommandsInfrastructure()
    .AddUtilities()
    .AddFactories()
    .AddPluginsInfrastructure()
    .AddApplication();

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:3000").AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", opts =>
    {
        opts.Authority = "http://localhost:5001";
        opts.Audience = "game‑api";
        opts.RequireHttpsMetadata = false;
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<MatchmakingHub>("/matchmaking/hub");
app.MapHub<GameHub>("/game/hub");

app.Run();