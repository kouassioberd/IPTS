using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPTS.Core.Entities
{
    public class HospitalPerformanceStat
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid HospitalId { get; set; }
        public Hospital Hospital { get; set; } = null!;
        public int TotalTransfersHandled { get; set; }
        public int TotalRequestsReceived { get; set; }
        public int TotalAccepted { get; set; }
        public int TotalDeclined { get; set; }
        public double AvgResponseTimeMinutes { get; set; }
        public double AvgTransferDurationMinutes { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
