using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPTS.Core.Entities
{
    /// <summary>
    /// AES-256 encrypted patient PII. Revealed ONLY to accepting hospital after Phase 3.
    /// </summary>
    public class ConfidentialPatientRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TransferRequestId { get; set; }
        public TransferRequest TransferRequest { get; set; } = null!;
        public string EncryptedPayload { get; set; } = string.Empty; // AES-256 JSON
        public bool IsRevealed { get; set; } = false;
        public DateTime? RevealedAt { get; set; }
        public Guid? RevealedToHospitalId { get; set; }
    }
}
