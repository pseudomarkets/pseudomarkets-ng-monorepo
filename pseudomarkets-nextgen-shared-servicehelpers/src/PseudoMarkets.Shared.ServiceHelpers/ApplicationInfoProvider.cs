using System.Reflection;

namespace PseudoMarkets.Shared.ServiceHelpers;

public static class ApplicationInfoProvider
{
    private const string BuildTimestampMetadataKey = "BuildTimestamp";

    public static ApplicationInfoResponse GetInfo<TMarker>()
    {
        return GetInfo(typeof(TMarker).Assembly);
    }

    public static ApplicationInfoResponse GetInfo(Assembly assembly)
    {
        var name = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product
            ?? assembly.GetName().Name
            ?? "Unknown";

        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "Unknown";

        var buildTimestamp = assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => string.Equals(
                attribute.Key,
                BuildTimestampMetadataKey,
                StringComparison.OrdinalIgnoreCase))?.Value
            ?? "Unknown";

        return new ApplicationInfoResponse(name, version, buildTimestamp);
    }
}
