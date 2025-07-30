using System.Text.Json.Serialization;
using App.Web.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.Converters.Add(new JsonFSharpConverter());
});
builder.Services.AddEndpointsApiExplorer();
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
    .AddApplication()
    .AddQuickGameApplicationHelpers()
    .AddSseInfrastructure();

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

app.Run();