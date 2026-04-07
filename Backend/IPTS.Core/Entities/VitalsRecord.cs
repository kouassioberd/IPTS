using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPTS.Core.Entities
{
    public class VitalsRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TransferRequestId { get; set; }
        public TransferRequest TransferRequest { get; set; } = null!;
        public Guid SubmittedByCrewId { get; set; }
        public string BloodPressure { get; set; } = string.Empty;
        public int HeartRate { get; set; }
        public int OxygenSaturation { get; set; }
        public int GlasgowComaScale { get; set; }
        public string Notes { get; set; } = string.Empty;
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    }
}
