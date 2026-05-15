using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PseudoMarkets.Shared.ServiceHelpers;

public static class HealthCheckJsonResponseWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new HealthCheckResponse(
            report.Status.ToString(),
            report.TotalDuration.ToString(),
            report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new HealthCheckEntryResponse(
                    entry.Value.Status.ToString(),
                    entry.Value.Description,
                    entry.Value.Duration.ToString(),
                    entry.Value.Data.ToDictionary(
                        dataEntry => dataEntry.Key,
                        dataEntry => (object?)dataEntry.Value))));

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, SerializerOptions));
    }
}
