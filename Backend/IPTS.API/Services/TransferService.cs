using BCrypt.Net;
using IPTS.Core.DTOs;
using IPTS.Core.Entities;
using IPTS.Core.Enums;
using IPTS.Core.Interfaces;
using IPTS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace IPTS.API.Services
{
   public class TransferService : ITransferService
   {
        private readonly AppDbContext _db;
        private readonly byte[] _aesKey;

        public TransferService(AppDbContext db, IConfiguration config)
        {
            _db = db;
            var keyStr = config["EncryptionSettings:Key"]
                ?? throw new InvalidOperationException(
                    "EncryptionSettings:Key is missing in appsettings.json");
            // AES-256 requires exactly 32 bytes
            _aesKey = Encoding.UTF8.GetBytes(keyStr.PadRight(32).Substring(0, 32));
        }

        //SubmitPatientDataAsync — the most important method
        public async Task<TransferRequestDto> SubmitPatientDataAsync(
            SubmitPatientDataRequest request,
            Guid sendingDoctorId,
            Guid sendingHospitalId)
        {
            // 1. Find the accepted response for this broadcast
            var response = await _db.HospitalResponses
                .FirstOrDefaultAsync(r =>
                    r.BroadcastId == request.BroadcastId &&
                    r.Response == ResponseType.Accepted)
                ?? throw new InvalidOperationException(
                    "No accepted response found for this broadcast.");

            // 2. Build the payload object (never stored as plaintext)
            var payload = new PatientPayload(
                PatientFullName: request.PatientFullName,
                DateOfBirth: request.DateOfBirth,
                Diagnosis: request.Diagnosis,
                Allergies: request.Allergies,
                CurrentMedications: request.CurrentMedications,
                AdditionalNotes: request.AdditionalNotes,
                FamilyContactName: request.FamilyContactName,
                FamilyContactPhone: request.FamilyContactPhone
            );

            // 3. Serialize to JSON then AES-256 encrypt
            var json = JsonSerializer.Serialize(payload);
            var encrypted = Encrypt(json);

            // 4. Create the TransferRequest record
            var transfer = new TransferRequest
            {
                Id = Guid.NewGuid(),
                BroadcastId = request.BroadcastId,
                SendingHospitalId = sendingHospitalId,
                ReceivingHospitalId = response.ReceivingHospitalId,
                Status = TransferStatus.Confirmed,
                ConfirmedAt = DateTime.UtcNow,
            };
            _db.TransferRequests.Add(transfer);

            // 5. Create the ConfidentialPatientRecord
            var record = new ConfidentialPatientRecord
            {
                Id = Guid.NewGuid(),
                TransferRequestId = transfer.Id,
                EncryptedPayload = encrypted,
                IsRevealed = false,
            };
            _db.PatientRecords.Add(record);

            // 6. Create family tracking token
            var token = new FamilyTrackingToken
            {
                Id = Guid.NewGuid(),
                TransferRequestId = transfer.Id,
                Token = GenerateToken(),
                FamilyContactName = request.FamilyContactName,
                SentToPhone = request.FamilyContactPhone,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(12),
            };
            _db.FamilyTrackingTokens.Add(token);

            // 7. Write audit log
            _db.AuditLogs.Add(new TransferAuditLog
            {
                Id = Guid.NewGuid(),
                TransferRequestId = transfer.Id,
                Action = "PatientDataSubmitted",
                PerformedByUserId = sendingDoctorId,
                PerformedByRole = "Doctor",
                Timestamp = DateTime.UtcNow,
                Details = $"Patient data encrypted and stored. Sending hospital: {sendingHospitalId}",
            });

            await _db.SaveChangesAsync();
            return MapToDto(transfer, record);
        }

        //GetPatientDataAsync — decrypt and reveal only to accepting hospital
        public async Task<DecryptedPatientDataDto?> GetPatientDataAsync(
            Guid transferRequestId,
            Guid callerHospitalId,
            Guid callerDoctorId)
        {
            var transfer = await _db.TransferRequests
                .Include(t => t.PatientRecord)
                .FirstOrDefaultAsync(t => t.Id == transferRequestId);

            if (transfer is null) return null;

            // Security check: only receiving hospital can access
            if (transfer.ReceivingHospitalId != callerHospitalId)
                throw new UnauthorizedAccessException(
                    "Only the accepting hospital can access patient data.");

            if (transfer.PatientRecord is null) return null;

            // Decrypt the AES-256 payload
            var json = Decrypt(transfer.PatientRecord.EncryptedPayload);
            var payload = JsonSerializer.Deserialize<PatientPayload>(json)
                ?? throw new InvalidOperationException("Failed to deserialize patient payload.");

            // Mark as revealed
            transfer.PatientRecord.IsRevealed = true;
            transfer.PatientRecord.RevealedAt = DateTime.UtcNow;
            transfer.PatientRecord.RevealedToHospitalId = callerHospitalId;

            // Audit log
            _db.AuditLogs.Add(new TransferAuditLog
            {
                Id = Guid.NewGuid(),
                TransferRequestId = transferRequestId,
                Action = "PatientDataRevealed",
                PerformedByUserId = callerDoctorId,
                PerformedByRole = "Doctor",
                Timestamp = DateTime.UtcNow,
                Details = $"Revealed to hospital {callerHospitalId}",
            });

            await _db.SaveChangesAsync();

            return new DecryptedPatientDataDto(
                TransferRequestId: transferRequestId,
                PatientFullName: payload.PatientFullName,
                DateOfBirth: payload.DateOfBirth,
                Diagnosis: payload.Diagnosis,
                Allergies: payload.Allergies,
                CurrentMedications: payload.CurrentMedications,
                AdditionalNotes: payload.AdditionalNotes,
                FamilyContactName: payload.FamilyContactName,
                FamilyContactPhone: payload.FamilyContactPhone,
                RevealedAt: DateTime.UtcNow
            );
        }

        //GetByIdAsync, GetByHospitalAsync, GetAuditLogAsync
        public async Task<TransferRequestDto?> GetByIdAsync(Guid id)
        {
            var t = await _db.TransferRequests
                .Include(t => t.PatientRecord)
                .Include(t => t.SendingHospital)
                .Include(t => t.ReceivingHospital)
                .FirstOrDefaultAsync(t => t.Id == id);
            return t is null ? null : MapToDto(t, t.PatientRecord);
        }

        public async Task<List<TransferRequestDto>> GetByHospitalAsync(Guid hospitalId)
        {
            var list = await _db.TransferRequests
                .Where(t => t.SendingHospitalId == hospitalId ||
                            t.ReceivingHospitalId == hospitalId)
                .Include(t => t.PatientRecord)
                .Include(t => t.SendingHospital)
                .Include(t => t.ReceivingHospital)
                .OrderByDescending(t => t.ConfirmedAt)
                .ToListAsync();
            return list.Select(t => MapToDto(t, t.PatientRecord)).ToList();
        }

        public async Task<List<AuditLogDto>> GetAuditLogAsync(
            Guid transferRequestId, Guid callerHospitalId)
        {
            var transfer = await _db.TransferRequests
                .FirstOrDefaultAsync(t => t.Id == transferRequestId);
            if (transfer is null) return [];
            // Both hospitals can read the audit log
            if (transfer.SendingHospitalId != callerHospitalId &&
                transfer.ReceivingHospitalId != callerHospitalId)
                return [];
            var logs = await _db.AuditLogs
                .Where(a => a.TransferRequestId == transferRequestId)
                .OrderBy(a => a.Timestamp)
                .ToListAsync();
            return logs.Select(a => new AuditLogDto(
                a.Id, a.Action, a.PerformedByRole, a.Timestamp, a.Details
            )).ToList();
        }

        //AES-256 Encrypt / Decrypt helpers
        private string Encrypt(string plainText)
        {
            using var aes = Aes.Create();
            aes.Key = _aesKey;
            aes.GenerateIV();
            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = encryptor.TransformFinalBlock(
                plainBytes, 0, plainBytes.Length);
            // Prepend the IV so we can decrypt later
            var result = new byte[aes.IV.Length + cipherBytes.Length];
            aes.IV.CopyTo(result, 0);
            cipherBytes.CopyTo(result, aes.IV.Length);
            return Convert.ToBase64String(result);
        }

        private string Decrypt(string cipherText)
        {
            var fullBytes = Convert.FromBase64String(cipherText);
            using var aes = Aes.Create();
            aes.Key = _aesKey;
            // First 16 bytes are the IV
            var iv = fullBytes[..16];
            var cipherBytes = fullBytes[16..];
            aes.IV = iv;
            using var decryptor = aes.CreateDecryptor();
            var plainBytes = decryptor.TransformFinalBlock(
                cipherBytes, 0, cipherBytes.Length);
            return Encoding.UTF8.GetString(plainBytes);
        }

        private static string GenerateToken()
        {
            var bytes = new byte[16]; // 128-bit
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-").Replace("/", "_").Replace("=", "");
        }

        // ── MAPPER ─────────────────────────────────────────────
        private static TransferRequestDto MapToDto(
            TransferRequest t, ConfidentialPatientRecord? record) => new(
            Id: t.Id,
            BroadcastId: t.BroadcastId,
            SendingHospitalName: t.SendingHospital?.Name ?? "",
            ReceivingHospitalName: t.ReceivingHospital?.Name ?? "",
            Status: t.Status,
            ConfirmedAt: t.ConfirmedAt,
            PatientDataSubmitted: record is not null,
            PatientDataRevealed: record?.IsRevealed ?? false,
            DeliveredAt: t.DeliveredAt
            );
   }
}
