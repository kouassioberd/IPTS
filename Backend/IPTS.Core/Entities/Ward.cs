using IPTS.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPTS.Core.Entities
{
    public class Ward
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid HospitalId { get; set; }
        public Hospital Hospital { get; set; } = null!;
        public string Name { get; set; } = string.Empty;
        public WardType Type { get; set; }
        public int TotalBeds { get; set; }

        public ICollection<Bed> Beds { get; set; } = [];
    }
}
