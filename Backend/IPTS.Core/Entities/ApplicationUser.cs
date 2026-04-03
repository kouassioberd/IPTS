using IPTS.Core.Enums;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPTS.Core.Entities
{
    /// <summary>
    /// Extends ASP.NET Identity user with hospital scoping.
    /// Used for all web app users: Doctor, Admin, Dispatcher.
    /// </summary>
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string FullName { get; set; } = string.Empty;
        public Guid HospitalId { get; set; }
        public Hospital Hospital { get; set; } = null!;
        public StaffRole Role { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
