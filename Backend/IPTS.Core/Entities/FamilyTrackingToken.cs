using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPTS.Core.Entities
{
    public class FamilyTrackingToken
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TransferRequestId { get; set; }
        public TransferRequest TransferRequest { get; set; } = null!;
        public string Token { get; set; } = string.Empty; // 128-bit random
        public string FamilyContactName { get; set; } = string.Empty;
        public string SentToPhone { get; set; } = string.Empty;
        public string? SentToEmail { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public bool IsRevoked { get; set; } = false;

        public bool IsActive => !IsRevoked && DateTime.UtcNow < ExpiresAt;
    }
}
