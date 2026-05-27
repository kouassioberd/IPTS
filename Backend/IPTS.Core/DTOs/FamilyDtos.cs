using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPTS.Core.DTOs
{
    public record FamilyTrackingDto(
    string PatientStatus,        // transfer status as string
    string SendingHospitalName,
    string ReceivingHospitalName,
    string ReceivingHospitalAddress,
    double AmbulanceLatitude,
    double AmbulanceLongitude,
    DateTime LastLocationUpdate,
    string AmbulanceUnit,
    bool IsExpired
    );

}
