using IPTS.Core.Interfaces;

namespace IPTS.API.Services
{

    // ══════════════════════════════════════════════════════════════════
    // DISTANCE SERVICE — Haversine Formula
    // Calculates real-world distance between two GPS coordinates.
    // No external library needed.
    // ══════════════════════════════════════════════════════════════════

    public class DistanceService : IDistanceService
    {
        private const double EarthRadiusMiles = 3958.8;

        public double GetDistanceMiles(double lat1, double lon1, double lat2, double lon2)
        {
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return EarthRadiusMiles * c;
        }

        public bool IsWithinRange(double lat1, double lon1, double lat2, double lon2, int maxMiles)
            => GetDistanceMiles(lat1, lon1, lat2, lon2) <= maxMiles;

        private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
    }

}
