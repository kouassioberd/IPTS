using IPTS.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPTS.Core.Interfaces
{
    public interface IFamilyService
    {
        Task<FamilyTrackingDto?> GetTrackingDataAsync(string token);
    }

}
