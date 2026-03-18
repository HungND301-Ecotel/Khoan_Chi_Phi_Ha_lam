using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Infrastructure.Middleware;

[ExcludeFromCodeCoverage]
public class ErrorResult : ProblemDetails
{
    public bool Success = false;
    public new string Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1";
    public Dictionary<string, string[]>? Errors { get; set; }

    [JsonIgnore]
    public new IDictionary<string, object?>? Extensions { get; set; }
}