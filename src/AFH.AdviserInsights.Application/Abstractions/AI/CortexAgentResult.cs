using System.Text.Json;

namespace AFH.AdviserInsights.Application.Abstractions.AI;

public sealed record CortexAgentResult(
    bool IsSuccess,
    int StatusCode,
    JsonElement Content);
