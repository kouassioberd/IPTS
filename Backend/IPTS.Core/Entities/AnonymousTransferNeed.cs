using IPTS.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPTS.Core.Entities
{
    /// <summary>
    /// Phase 1: Contains ZERO patient PII. Broadcast to all candidate hospitals.
    /// </summary>
    public class AnonymousTransferNeed
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid SendingHospitalId { get; set; }
        public Hospital SendingHospital { get; set; } = null!;
        public Guid SendingDoctorId { get; set; }
        public ApplicationUser SendingDoctor { get; set; } = null!;

        // Medical requirements only — no patient identity
        public string BedTypeRequired { get; set; } = string.Empty;
        public string EquipmentNeeded { get; set; } = string.Empty;
        public string InsuranceType { get; set; } = string.Empty;
        public int MaxDistanceMiles { get; set; }
        public UrgencyLevel Urgency { get; set; }
        public BroadcastStatus Status { get; set; } = BroadcastStatus.Active;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<HospitalResponse> Responses { get; set; } = [];
    }
}
