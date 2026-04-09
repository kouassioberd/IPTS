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
        public DbSet<TransferRequest> TransferRequests { get; set; }
        public DbSet<AnonymousTransferNeed> AnonymousTransferNeeds { get; set; }
        public DbSet<ConfidentialPatientRecord> PatientRecords { get; set; }
        public DbSet<FamilyTrackingToken> FamilyTrackingTokens { get; set; }
        public DbSet<HospitalResponse> HospitalResponses { get; set; }
        public DbSet<TransferAuditLog> AuditLogs { get; set; }
        public DbSet<VitalsRecord> VitalsRecords { get; set; }

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

            // ── ANONYMOUS TRANSFER NEED ───────────────────────────────
            builder.Entity<AnonymousTransferNeed>(e =>
            {
                e.HasKey(n => n.Id);
                e.HasOne(n => n.SendingHospital)
                 .WithMany().HasForeignKey(n => n.SendingHospitalId)
                 .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(n => n.SendingDoctor)
                 .WithMany().HasForeignKey(n => n.SendingDoctorId)
                 .OnDelete(DeleteBehavior.Restrict);
                e.HasMany(n => n.Responses).WithOne(r => r.Broadcast)
                 .HasForeignKey(r => r.BroadcastId).OnDelete(DeleteBehavior.Cascade);
            });

            // ── HOSPITAL RESPONSE ─────────────────────────────────────
            builder.Entity<HospitalResponse>(e =>
            {
                e.HasKey(r => r.Id);
                e.HasOne(r => r.ReceivingHospital)
                 .WithMany().HasForeignKey(r => r.ReceivingHospitalId)
                 .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(r => r.RespondingDoctor)
                 .WithMany().HasForeignKey(r => r.RespondingDoctorId)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            // ── TRANSFER REQUEST ──────────────────────────────────────
            builder.Entity<TransferRequest>(e =>
            {
                e.HasKey(t => t.Id);
                e.HasOne(t => t.SendingHospital)
                 .WithMany().HasForeignKey(t => t.SendingHospitalId)
                 .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(t => t.ReceivingHospital)
                 .WithMany().HasForeignKey(t => t.ReceivingHospitalId)
                 .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(t => t.AssignedAmbulance)
                 .WithMany().HasForeignKey(t => t.AssignedAmbulanceId)
                 .OnDelete(DeleteBehavior.SetNull);
                e.HasOne(t => t.PatientRecord).WithOne(p => p.TransferRequest)
                 .HasForeignKey<ConfidentialPatientRecord>(p => p.TransferRequestId);
                e.HasOne(t => t.TrackingToken).WithOne(tk => tk.TransferRequest)
                 .HasForeignKey<FamilyTrackingToken>(tk => tk.TransferRequestId);
            });

            // ── CONFIDENTIAL PATIENT RECORD ───────────────────────────
            builder.Entity<ConfidentialPatientRecord>(e =>
            {
                e.HasKey(p => p.Id);
                // Encrypted payload stored as TEXT — never query inside it
                e.Property(p => p.EncryptedPayload).HasColumnType("text");
            });

            // ── VITALS ────────────────────────────────────────────────
            builder.Entity<VitalsRecord>(e =>
            {
                e.HasKey(v => v.Id);
                e.HasOne(v => v.TransferRequest)
                 .WithMany(t => t.Vitals).HasForeignKey(v => v.TransferRequestId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── AUDIT LOG ─────────────────────────────────────────────
            builder.Entity<TransferAuditLog>(e =>
            {
                e.HasKey(a => a.Id);
                e.HasOne(a => a.TransferRequest)
                 .WithMany(t => t.AuditLogs).HasForeignKey(a => a.TransferRequestId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── FAMILY TRACKING TOKEN ─────────────────────────────────
            builder.Entity<FamilyTrackingToken>(e =>
            {
                e.HasKey(t => t.Id);
                e.HasIndex(t => t.Token).IsUnique();
                e.Property(t => t.Token).IsRequired().HasMaxLength(256);
            });

        }
    }
}
