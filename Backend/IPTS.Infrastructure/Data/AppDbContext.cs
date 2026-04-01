using IPTS.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPTS.Infrastructure.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // ── DB SETS ───────────────────────────────────────────────────
        public DbSet<Hospital> Hospitals { get; set; }
        public DbSet<Ward> Wards { get; set; }
        public DbSet<Bed> Beds { get; set; }
        public DbSet<Ambulance> Ambulances { get; set; }
        public DbSet<AmbulanceCrew> AmbulanceCrews { get; set; }
        public DbSet<HospitalPerformanceStat> HospitalPerformanceStats { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ── HOSPITAL ──────────────────────────────────────────────
            builder.Entity<Hospital>(e =>
            {
                e.HasKey(h => h.Id);
                e.Property(h => h.Name).IsRequired().HasMaxLength(200);
                e.Property(h => h.Phone).HasMaxLength(30);
                e.HasMany(h => h.Wards).WithOne(w => w.Hospital)
                 .HasForeignKey(w => w.HospitalId).OnDelete(DeleteBehavior.Cascade);
                e.HasMany(h => h.Ambulances).WithOne(a => a.Hospital)
                 .HasForeignKey(a => a.HospitalId).OnDelete(DeleteBehavior.Restrict);
            });

            // ── WARD & BED ────────────────────────────────────────────
            builder.Entity<Ward>(e =>
            {
                e.HasKey(w => w.Id);
                e.HasMany(w => w.Beds).WithOne(b => b.Ward)
                 .HasForeignKey(b => b.WardId).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Bed>(e =>
            {
                e.HasKey(b => b.Id);
                e.Property(b => b.BedNumber).IsRequired().HasMaxLength(20);
            });

            // ── AMBULANCE & CREW ──────────────────────────────────────
            builder.Entity<Ambulance>(e =>
            {
                e.HasKey(a => a.Id);
                e.Property(a => a.UnitNumber).IsRequired().HasMaxLength(50);
                e.HasMany(a => a.Crew).WithOne(c => c.Ambulance)
                 .HasForeignKey(c => c.AmbulanceId).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<AmbulanceCrew>(e =>
            {
                e.HasKey(c => c.Id);
                e.Property(c => c.Email).IsRequired().HasMaxLength(200);
                e.HasIndex(c => c.Email).IsUnique();
            });       
            // ── PERFORMANCE STATS ─────────────────────────────────────
            builder.Entity<HospitalPerformanceStat>(e =>
            {
                e.HasKey(s => s.Id);
                e.HasOne(s => s.Hospital)
                 .WithMany().HasForeignKey(s => s.HospitalId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── REFRESH TOKENS ────────────────────────────────────────
            builder.Entity<RefreshToken>(e =>
            {
                e.HasKey(r => r.Id);
                e.HasIndex(r => r.Token).IsUnique();
            });

            // ── APPLICATION USER → HOSPITAL ───────────────────────────
            builder.Entity<ApplicationUser>(e =>
            {
                e.HasOne(u => u.Hospital)
                 .WithMany(h => h.Staff).HasForeignKey(u => u.HospitalId)
                 .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
