using IPTS.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPTS.Core.Entities
{
    /// <summary>
    /// Phase 3: Created after both hospitals confirm. Full transfer record.
    /// </summary>
    public class TransferRequest
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid BroadcastId { get; set; }
        public AnonymousTransferNeed Broadcast { get; set; } = null!;
        public Guid SendingHospitalId { get; set; }
        public Hospital SendingHospital { get; set; } = null!;
        public Guid ReceivingHospitalId { get; set; }
        public Hospital ReceivingHospital { get; set; } = null!;
        public Guid? AssignedAmbulanceId { get; set; }
        public Ambulance? AssignedAmbulance { get; set; }
        public TransferStatus Status { get; set; } = TransferStatus.Confirmed;
        public DateTime ConfirmedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeliveredAt { get; set; }

        public ConfidentialPatientRecord? PatientRecord { get; set; }
        public FamilyTrackingToken? TrackingToken { get; set; }
        public ICollection<VitalsRecord> Vitals { get; set; } = [];
        public ICollection<TransferAuditLog> AuditLogs { get; set; } = [];
    }
}
