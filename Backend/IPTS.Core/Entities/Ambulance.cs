using IPTS.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPTS.Core.Entities
{
    public class Ambulance
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid HospitalId { get; set; }
        public Hospital Hospital { get; set; } = null!;
        public string UnitNumber { get; set; } = string.Empty;
        public AmbulanceStatus Status { get; set; } = AmbulanceStatus.Available;
        public double CurrentLatitude { get; set; }
        public double CurrentLongitude { get; set; }
        public DateTime LastLocationUpdate { get; set; } = DateTime.UtcNow;

        public ICollection<AmbulanceCrew> Crew { get; set; } = [];
    }
}
