using IPTS.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPTS.Core.Entities
{
    /// <summary>
    /// Phase 2: Each receiving hospital's accept/decline response.
    /// </summary>
    public class HospitalResponse
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid BroadcastId { get; set; }
        public AnonymousTransferNeed Broadcast { get; set; } = null!;
        public Guid ReceivingHospitalId { get; set; }
        public Hospital ReceivingHospital { get; set; } = null!;
        public Guid? RespondingDoctorId { get; set; }
        public ApplicationUser? RespondingDoctor { get; set; }
        public ResponseType Response { get; set; } = ResponseType.Pending;
        public string? DeclineReason { get; set; }
        public DateTime? RespondedAt { get; set; }
    }
}
