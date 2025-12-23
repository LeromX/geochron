using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using WpfPoint = System.Windows.Point;

namespace GeochronScreensaver.Rendering.Globe;

/// <summary>
/// Creates a sphere mesh with UV coordinates for equirectangular texture mapping.
/// UV mapping follows the standard equirectangular projection where:
/// - u = (longitude + 180) / 360
/// - v = (90 - latitude) / 180
/// </summary>
public static class EarthSphereGeometry
{
    /// <summary>
    /// Creates a sphere mesh with UV coordinates for texture mapping.
    /// </summary>
    /// <param name="radius">Radius of the sphere</param>
    /// <param name="latDivisions">Number of latitude divisions (horizontal slices)</param>
    /// <param name="lonDivisions">Number of longitude divisions (vertical slices)</param>
    /// <returns>A MeshGeometry3D representing the sphere</returns>
    public static MeshGeometry3D CreateSphereMesh(double radius, int latDivisions, int lonDivisions)
    {
        var mesh = new MeshGeometry3D();

        // Generate vertices
        for (int lat = 0; lat <= latDivisions; lat++)
        {
            double theta = lat * Math.PI / latDivisions; // 0 to PI (north pole to south pole)
            double sinTheta = Math.Sin(theta);
            double cosTheta = Math.Cos(theta);

            for (int lon = 0; lon <= lonDivisions; lon++)
            {
                double phi = lon * 2.0 * Math.PI / lonDivisions; // 0 to 2*PI
                double sinPhi = Math.Sin(phi);
                double cosPhi = Math.Cos(phi);

                // Spherical to Cartesian coordinates
                double x = radius * sinTheta * cosPhi;
                double y = radius * cosTheta;
                double z = radius * sinTheta * sinPhi;

                // Add vertex position
                mesh.Positions.Add(new Point3D(x, y, z));

                // Add normal (points outward from center)
                mesh.Normals.Add(new Vector3D(sinTheta * cosPhi, cosTheta, sinTheta * sinPhi));

                // Calculate UV coordinates for equirectangular texture mapping
                // Convert theta, phi to latitude, longitude
                double latitude = 90.0 - (theta * 180.0 / Math.PI);
                double longitude = (phi * 180.0 / Math.PI) - 180.0;

                // Normalize to 0-1 range
                // Flip u horizontally (1.0 - u) to correct mirrored texture
                double u = 1.0 - (longitude + 180.0) / 360.0;
                double v = (90.0 - latitude) / 180.0;

                mesh.TextureCoordinates.Add(new WpfPoint(u, v));
            }
        }

        // Generate triangle indices
        for (int lat = 0; lat < latDivisions; lat++)
        {
            for (int lon = 0; lon < lonDivisions; lon++)
            {
                int first = lat * (lonDivisions + 1) + lon;
                int second = first + lonDivisions + 1;

                // First triangle (counter-clockwise when viewed from outside)
                mesh.TriangleIndices.Add(first);
                mesh.TriangleIndices.Add(first + 1);
                mesh.TriangleIndices.Add(second);

                // Second triangle
                mesh.TriangleIndices.Add(second);
                mesh.TriangleIndices.Add(first + 1);
                mesh.TriangleIndices.Add(second + 1);
            }
        }

        return mesh;
    }
}
