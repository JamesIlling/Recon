namespace LocationManagement.Api.Services;

/// <summary>
/// Reprojects coordinates from a source coordinate reference system (CRS) to WGS84 (EPSG:4326).
/// </summary>
public interface ICoordinateReprojectionService
{
    /// <summary>
    /// Checks whether the specified SRID is supported for reprojection to WGS84.
    /// </summary>
    /// <param name="srid">The Spatial Reference ID to check.</param>
    /// <returns>True if the SRID is supported; otherwise, false.</returns>
    bool IsSridSupported(int srid);

    /// <summary>
    /// Reprojects a coordinate pair from a source CRS to WGS84 (EPSG:4326) and rounds to 6 decimal places.
    /// </summary>
    /// <param name="latitude">The latitude in the source CRS.</param>
    /// <param name="longitude">The longitude in the source CRS.</param>
    /// <param name="sourceSrid">The SRID of the source CRS.</param>
    /// <returns>A tuple of (latitude, longitude) in WGS84, rounded to 6 decimal places.</returns>
    /// <exception cref="ArgumentException">Thrown if the source SRID is not supported.</exception>
    (double Latitude, double Longitude) ReprojectToWgs84(double latitude, double longitude, int sourceSrid);
}
