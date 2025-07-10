using App.Domain.Competition;

namespace App.Application.Competition.Engine.Creation;

public sealed record Context(Guid EngineId, Dictionary<string, object> RawOptions, Hill Hill);