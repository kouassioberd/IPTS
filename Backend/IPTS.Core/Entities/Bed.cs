using IPTS.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPTS.Core.Entities
{
    public class Bed
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid WardId { get; set; }
        public Ward Ward { get; set; } = null!;
        public string BedNumber { get; set; } = string.Empty;
        public BedStatus Status { get; set; } = BedStatus.Available;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
