using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPTS.Core.Entities
{
    public class Hospital
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Phone { get; set; } = string.Empty;
        public string AcceptedInsuranceTypes { get; set; } = string.Empty; // comma-separated
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<Ward> Wards { get; set; } = [];
        public ICollection<Ambulance> Ambulances { get; set; } = [];
        public ICollection<ApplicationUser> Staff { get; set; } = [];
    }
}
