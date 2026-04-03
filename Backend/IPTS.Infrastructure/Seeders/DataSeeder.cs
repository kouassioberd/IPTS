using IPTS.Core.Entities;
using IPTS.Core.Enums;
using IPTS.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPTS.Infrastructure.Seeders
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(AppDbContext db, UserManager<ApplicationUser> userManager)
        {
            // Apply any pending migrations automatically
            await db.Database.MigrateAsync();

            var hospital1Id = Guid.Parse("a1b2c3d4-0000-0000-0000-000000000001");
            var hospital2Id = Guid.Parse("a1b2c3d4-0000-0000-0000-000000000002");

            // ── HOSPITALS ─────────────────────────────────────────────
            if (!await db.Hospitals.AnyAsync())
            {
                await db.Hospitals.AddRangeAsync(new List<Hospital>
            {
                new Hospital
                {
                    Id                     = hospital1Id,
                    Name                   = "City General Hospital",
                    Address                = "12 Main Street, Downtown",
                    Latitude               = 33.8938,
                    Longitude              = 35.5018,
                    Phone                  = "+961-1-100100",
                    AcceptedInsuranceTypes = "Medicare,Medicaid,BlueCross",
                    IsActive               = true,
                    CreatedAt              = DateTime.UtcNow
                },
                new Hospital
                {
                    Id                     = hospital2Id,
                    Name                   = "Saint George Medical Center",
                    Address                = "45 Hospital Road, Ashrafieh",
                    Latitude               = 33.9000,
                    Longitude              = 35.5100,
                    Phone                  = "+961-1-200200",
                    AcceptedInsuranceTypes = "Medicare,Aetna,Cigna",
                    IsActive               = true,
                    CreatedAt              = DateTime.UtcNow
                }
            });
                await db.SaveChangesAsync();
                Console.WriteLine("✅ Hospitals seeded.");
            }

            // ── WARDS + BEDS ──────────────────────────────────────────────────
            if (!await db.Wards.AnyAsync())
            {
                var wardDefinitions = new List<(Guid HospId, string Name, WardType Type, int BedCount)>
                {
                      (hospital1Id, "ICU Ward A",     WardType.ICU,       10),
                      (hospital1Id, "Emergency Room", WardType.ER,        20),
                      (hospital1Id, "General Ward 1", WardType.General,   30),
                      (hospital2Id, "Cardiac ICU",    WardType.ICU,        8),
                      (hospital2Id, "Pediatric Ward", WardType.Pediatric, 15),
                      (hospital2Id, "Surgical Ward",  WardType.Surgery,   12),
                };

                foreach (var (hospId, name, type, bedCount) in wardDefinitions)
                {
                    var ward = new Ward
                    {
                        Id = Guid.NewGuid(),
                        HospitalId = hospId,
                        Name = name,
                        Type = type,
                        TotalBeds = bedCount,
                    };

                    db.Wards.Add(ward);

                    // Generate actual bed rows for this ward
                    var prefix = name.Length >= 3 ? name[..3].ToUpper() : name.ToUpper();
                    for (int i = 1; i <= bedCount; i++)
                    {
                        db.Beds.Add(new Bed
                        {
                            Id = Guid.NewGuid(),
                            WardId = ward.Id,
                            BedNumber = $"{prefix}-{i:D2}",
                            Status = BedStatus.Available,
                        });
                    }
                }

                await db.SaveChangesAsync();
                Console.WriteLine("✅ Wards and beds seeded.");
            }

            // ── AMBULANCES ────────────────────────────────────────────
            if (!await db.Ambulances.AnyAsync())
            {
                await db.Ambulances.AddRangeAsync(new List<Ambulance>
            {
                new Ambulance { Id = Guid.NewGuid(), HospitalId = hospital1Id, UnitNumber = "AMB-001", Status = AmbulanceStatus.Available },
                new Ambulance { Id = Guid.NewGuid(), HospitalId = hospital1Id, UnitNumber = "AMB-002", Status = AmbulanceStatus.Available },
                new Ambulance { Id = Guid.NewGuid(), HospitalId = hospital2Id, UnitNumber = "AMB-003", Status = AmbulanceStatus.Available },
            });
                await db.SaveChangesAsync();
                Console.WriteLine("✅ Ambulances seeded.");
            }

            // ── PERFORMANCE STATS ─────────────────────────────────────
            if (!await db.HospitalPerformanceStats.AnyAsync())
            {
                await db.HospitalPerformanceStats.AddRangeAsync(new List<HospitalPerformanceStat>
            {
                new HospitalPerformanceStat { Id = Guid.NewGuid(), HospitalId = hospital1Id },
                new HospitalPerformanceStat { Id = Guid.NewGuid(), HospitalId = hospital2Id },
            });
                await db.SaveChangesAsync();
                Console.WriteLine("✅ Performance stats seeded.");
            }

            // ── USERS ─────────────────────────────────────────────────
            // Each user: email, password, full name, role, hospital
            // Password for ALL test users: Test@1234
            if (!await db.Users.AnyAsync())
            {
                var testUsers = new List<(string FullName, string Email, string Password, StaffRole Role, Guid HospitalId)>
            {
                // Hospital 1 users
                ("Alice Admin",       "admin@cityhospital.com",      "Test@1234", StaffRole.Admin,      hospital1Id),
                ("Dr. John Sender",   "doctor.send@cityhospital.com","Test@1234", StaffRole.Doctor,     hospital1Id),
                ("Mike Dispatcher",   "dispatch@cityhospital.com",   "Test@1234", StaffRole.Dispatcher, hospital1Id),

                // Hospital 2 users
                ("Bob Admin",         "admin@stgeorge.com",          "Test@1234", StaffRole.Admin,      hospital2Id),
                ("Dr. Sarah Receiver","doctor.recv@stgeorge.com",    "Test@1234", StaffRole.Doctor,     hospital2Id),
                ("Lisa Dispatcher",   "dispatch@stgeorge.com",       "Test@1234", StaffRole.Dispatcher, hospital2Id),
            };

                foreach (var (fullName, email, password, role, hospitalId) in testUsers)
                {
                    // Skip if user already exists
                    if (await userManager.FindByEmailAsync(email) is not null)
                        continue;

                    var user = new ApplicationUser
                    {
                        FullName = fullName,
                        Email = email,
                        UserName = email,
                        HospitalId = hospitalId,
                        Role = role,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                    };

                    // CreateAsync handles password hashing automatically
                    var result = await userManager.CreateAsync(user, password);

                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, role.ToString());
                        Console.WriteLine($"✅ User seeded: {email} [{role}]");
                    }
                    else
                    {
                        // Print errors so you know what went wrong
                        foreach (var error in result.Errors)
                            Console.WriteLine($"❌ Failed to seed {email}: {error.Description}");
                    }
                }
            }

            // ── AMBULANCE CREW ────────────────────────────────────────
            // Driver and Paramedic use a separate table (not Identity)
            // so we hash their passwords manually with BCrypt
            if (!await db.AmbulanceCrews.AnyAsync())
            {
                // Get ambulance IDs that were just seeded
                var amb1 = await db.Ambulances.FirstAsync(a => a.UnitNumber == "AMB-001");
                var amb2 = await db.Ambulances.FirstAsync(a => a.UnitNumber == "AMB-002");
                var amb3 = await db.Ambulances.FirstAsync(a => a.UnitNumber == "AMB-003");

                var crews = new List<AmbulanceCrew>
            {
                new AmbulanceCrew
                {
                    Id           = Guid.NewGuid(),
                    AmbulanceId  = amb1.Id,
                    FullName     = "Tom Driver",
                    Email        = "driver1@cityhospital.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@1234"),
                    Role         = CrewRole.Driver,
                    IsActive     = true
                },
                new AmbulanceCrew
                {
                    Id           = Guid.NewGuid(),
                    AmbulanceId  = amb1.Id,
                    FullName     = "Nurse Emma",
                    Email        = "paramedic1@cityhospital.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@1234"),
                    Role         = CrewRole.Paramedic,
                    IsActive     = true
                },
                new AmbulanceCrew
                {
                    Id           = Guid.NewGuid(),
                    AmbulanceId  = amb2.Id,
                    FullName     = "Jake Driver",
                    Email        = "driver2@cityhospital.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@1234"),
                    Role         = CrewRole.Driver,
                    IsActive     = true
                },
                new AmbulanceCrew
                {
                    Id           = Guid.NewGuid(),
                    AmbulanceId  = amb3.Id,
                    FullName     = "Chris Driver",
                    Email        = "driver3@stgeorge.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@1234"),
                    Role         = CrewRole.Driver,
                    IsActive     = true
                },
                new AmbulanceCrew
                {
                    Id           = Guid.NewGuid(),
                    AmbulanceId  = amb3.Id,
                    FullName     = "Nurse Diana",
                    Email        = "paramedic2@stgeorge.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@1234"),
                    Role         = CrewRole.Paramedic,
                    IsActive     = true
                },
            };

                await db.AmbulanceCrews.AddRangeAsync(crews);
                await db.SaveChangesAsync();
                Console.WriteLine("✅ Ambulance crews seeded.");
            }
        }
    }
}
