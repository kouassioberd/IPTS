using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPTS.Core.Entities
{
    public class TransferAuditLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TransferRequestId { get; set; }
        public TransferRequest TransferRequest { get; set; } = null!;
        public string Action { get; set; } = string.Empty;
        public Guid PerformedByUserId { get; set; }
        public string PerformedByRole { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Details { get; set; } = string.Empty;
    }
}
