using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace LocationManagement.Api.Telemetry;

/// <summary>
/// Central registry for custom ActivitySource and Meter used across the Location Management API.
/// Register these with the OTel provider in Program.cs via AddServiceDefaults().
/// </summary>
public static class LocationManagementTelemetry
{
    /// <summary>The ActivitySource for creating custom trace spans.</summary>
    public static readonly ActivitySource ActivitySource = new("LocationManagement", "1.0.0");

    /// <summary>The Meter for recording custom metrics.</summary>
    public static readonly Meter Meter = new("LocationManagement", "1.0.0");

    /// <summary>Histogram for Location create operation durations in milliseconds.</summary>
    public static readonly Histogram<double> LocationCreateDuration =
        Meter.CreateHistogram<double>("location.create.duration", "ms", "Duration of location create operations.");

    /// <summary>Histogram for Location edit operation durations in milliseconds.</summary>
    public static readonly Histogram<double> LocationEditDuration =
        Meter.CreateHistogram<double>("location.edit.duration", "ms", "Duration of location edit operations.");

    /// <summary>Counter for image upload operations.</summary>
    public static readonly Counter<long> ImageUploadCount =
        Meter.CreateCounter<long>("image.upload.count", description: "Number of image uploads.");
}
