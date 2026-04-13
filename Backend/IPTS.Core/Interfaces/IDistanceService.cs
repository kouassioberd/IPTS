using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPTS.Core.Interfaces
{
    public interface IDistanceService
    {
        double GetDistanceMiles(double lat1, double lon1, double lat2, double lon2);
        bool IsWithinRange(double lat1, double lon1, double lat2, double lon2, int maxMiles);
    }
}
