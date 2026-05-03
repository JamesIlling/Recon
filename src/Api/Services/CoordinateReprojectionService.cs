using GeoAPI.CoordinateSystems;
using GeoAPI.CoordinateSystems.Transformations;
using ProjNet;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace LocationManagement.Api.Services;

/// <summary>
/// Implements coordinate reprojection from various CRS to WGS84 (EPSG:4326) using ProjNet4GeoAPI.
/// </summary>
public sealed class CoordinateReprojectionService : ICoordinateReprojectionService
{
    private readonly CoordinateSystemFactory _csFactory;
    private readonly CoordinateTransformationFactory _ctFactory;
    private readonly ICoordinateSystem _wgs84;

    /// <summary>
    /// Initializes a new instance of the <see cref="CoordinateReprojectionService"/> class.
    /// </summary>
    public CoordinateReprojectionService()
    {
        _csFactory = new CoordinateSystemFactory();
        _ctFactory = new CoordinateTransformationFactory();
        _wgs84 = _csFactory.CreateFromWkt("GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]]");
    }

    /// <summary>
    /// Checks whether the specified SRID is supported for reprojection to WGS84.
    /// </summary>
    /// <param name="srid">The Spatial Reference ID to check.</param>
    /// <returns>True if the SRID is supported; otherwise, false.</returns>
    public bool IsSridSupported(int srid)
    {
        // WGS84 is always supported
        if (srid == 4326)
        {
            return true;
        }

        try
        {
            // Attempt to create a coordinate system from the SRID
            // If successful, the SRID is supported
            var cs = _csFactory.CreateFromWkt(GetWktForSrid(srid));
            return cs != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Reprojects a coordinate pair from a source CRS to WGS84 (EPSG:4326) and rounds to 6 decimal places.
    /// </summary>
    /// <param name="latitude">The latitude in the source CRS.</param>
    /// <param name="longitude">The longitude in the source CRS.</param>
    /// <param name="sourceSrid">The SRID of the source CRS.</param>
    /// <returns>A tuple of (latitude, longitude) in WGS84, rounded to 6 decimal places.</returns>
    /// <exception cref="ArgumentException">Thrown if the source SRID is not supported.</exception>
    public (double Latitude, double Longitude) ReprojectToWgs84(double latitude, double longitude, int sourceSrid)
    {
        // If already WGS84, just round and return
        if (sourceSrid == 4326)
        {
            return (
                Math.Round(latitude, 6, MidpointRounding.AwayFromZero),
                Math.Round(longitude, 6, MidpointRounding.AwayFromZero)
            );
        }

        if (!IsSridSupported(sourceSrid))
        {
            throw new ArgumentException($"SRID {sourceSrid} is not supported for reprojection to WGS84.", nameof(sourceSrid));
        }

        try
        {
            var sourceCs = _csFactory.CreateFromWkt(GetWktForSrid(sourceSrid));
            var transformation = _ctFactory.CreateFromCoordinateSystems(sourceCs, _wgs84);

            // ProjNet expects [x, y] which is [longitude, latitude]
            var sourceCoords = new[] { longitude, latitude };
            var transformedCoords = transformation.MathTransform.Transform(sourceCoords);

            // transformedCoords is [longitude, latitude] in WGS84
            var wgs84Longitude = transformedCoords[0];
            var wgs84Latitude = transformedCoords[1];

            // Round to 6 decimal places
            return (
                Math.Round(wgs84Latitude, 6, MidpointRounding.AwayFromZero),
                Math.Round(wgs84Longitude, 6, MidpointRounding.AwayFromZero)
            );
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Failed to reproject coordinates from SRID {sourceSrid} to WGS84.", nameof(sourceSrid), ex);
        }
    }

    /// <summary>
    /// Gets the WKT representation for a given SRID.
    /// This is a simplified implementation that handles common SRIDs.
    /// For a production system, this should query a spatial reference database.
    /// </summary>
    private static string GetWktForSrid(int srid)
    {
        // Common SRID definitions
        return srid switch
        {
            4326 => "GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]]",
            3857 => "PROJCS[\"WGS 84 / Pseudo-Mercator\",GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"Mercator_1SP\"],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AXIS[\"Easting\",EAST],AXIS[\"Northing\",NORTH],AUTHORITY[\"EPSG\",\"3857\"]]",
            2154 => "PROJCS[\"Lambert 93\",GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"Lambert_Conformal_Conic_2SP\"],PARAMETER[\"standard_parallel_1\",49],PARAMETER[\"standard_parallel_2\",44],PARAMETER[\"latitude_of_origin\",46.5],PARAMETER[\"central_meridian\",3],PARAMETER[\"false_easting\",700000],PARAMETER[\"false_northing\",6000000],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AXIS[\"Easting\",EAST],AXIS[\"Northing\",NORTH],AUTHORITY[\"EPSG\",\"2154\"]]",
            _ => throw new ArgumentException($"SRID {srid} is not in the supported list.", nameof(srid))
        };
    }
}
