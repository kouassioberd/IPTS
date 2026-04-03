using IPTS.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPTS.Core.Entities
{
    public class AmbulanceCrew
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AmbulanceId { get; set; }
        public Ambulance Ambulance { get; set; } = null!;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public CrewRole Role { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
