using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IPTS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTransferRequestAnonymTransferConfidentialPRecordFamillyTTokenHospitalResTansferALogVitalsRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnonymousTransferNeeds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SendingHospitalId = table.Column<Guid>(type: "uuid", nullable: false),
                    SendingDoctorId = table.Column<Guid>(type: "uuid", nullable: false),
                    BedTypeRequired = table.Column<string>(type: "text", nullable: false),
                    EquipmentNeeded = table.Column<string>(type: "text", nullable: false),
                    InsuranceType = table.Column<string>(type: "text", nullable: false),
                    MaxDistanceMiles = table.Column<int>(type: "integer", nullable: false),
                    Urgency = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnonymousTransferNeeds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnonymousTransferNeeds_AspNetUsers_SendingDoctorId",
                        column: x => x.SendingDoctorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AnonymousTransferNeeds_Hospitals_SendingHospitalId",
                        column: x => x.SendingHospitalId,
                        principalTable: "Hospitals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HospitalResponses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BroadcastId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceivingHospitalId = table.Column<Guid>(type: "uuid", nullable: false),
                    RespondingDoctorId = table.Column<Guid>(type: "uuid", nullable: true),
                    Response = table.Column<int>(type: "integer", nullable: false),
                    DeclineReason = table.Column<string>(type: "text", nullable: true),
                    RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HospitalResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HospitalResponses_AnonymousTransferNeeds_BroadcastId",
                        column: x => x.BroadcastId,
                        principalTable: "AnonymousTransferNeeds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HospitalResponses_AspNetUsers_RespondingDoctorId",
                        column: x => x.RespondingDoctorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_HospitalResponses_Hospitals_ReceivingHospitalId",
                        column: x => x.ReceivingHospitalId,
                        principalTable: "Hospitals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TransferRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BroadcastId = table.Column<Guid>(type: "uuid", nullable: false),
                    SendingHospitalId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceivingHospitalId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAmbulanceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransferRequests_Ambulances_AssignedAmbulanceId",
                        column: x => x.AssignedAmbulanceId,
                        principalTable: "Ambulances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TransferRequests_AnonymousTransferNeeds_BroadcastId",
                        column: x => x.BroadcastId,
                        principalTable: "AnonymousTransferNeeds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TransferRequests_Hospitals_ReceivingHospitalId",
                        column: x => x.ReceivingHospitalId,
                        principalTable: "Hospitals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransferRequests_Hospitals_SendingHospitalId",
                        column: x => x.SendingHospitalId,
                        principalTable: "Hospitals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransferRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    PerformedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PerformedByRole = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Details = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_TransferRequests_TransferRequestId",
                        column: x => x.TransferRequestId,
                        principalTable: "TransferRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FamilyTrackingTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransferRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FamilyContactName = table.Column<string>(type: "text", nullable: false),
                    SentToPhone = table.Column<string>(type: "text", nullable: false),
                    SentToEmail = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyTrackingTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FamilyTrackingTokens_TransferRequests_TransferRequestId",
                        column: x => x.TransferRequestId,
                        principalTable: "TransferRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransferRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    EncryptedPayload = table.Column<string>(type: "text", nullable: false),
                    IsRevealed = table.Column<bool>(type: "boolean", nullable: false),
                    RevealedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevealedToHospitalId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientRecords_TransferRequests_TransferRequestId",
                        column: x => x.TransferRequestId,
                        principalTable: "TransferRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VitalsRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransferRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedByCrewId = table.Column<Guid>(type: "uuid", nullable: false),
                    BloodPressure = table.Column<string>(type: "text", nullable: false),
                    HeartRate = table.Column<int>(type: "integer", nullable: false),
                    OxygenSaturation = table.Column<int>(type: "integer", nullable: false),
                    GlasgowComaScale = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VitalsRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VitalsRecords_TransferRequests_TransferRequestId",
                        column: x => x.TransferRequestId,
                        principalTable: "TransferRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnonymousTransferNeeds_SendingDoctorId",
                table: "AnonymousTransferNeeds",
                column: "SendingDoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_AnonymousTransferNeeds_SendingHospitalId",
                table: "AnonymousTransferNeeds",
                column: "SendingHospitalId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TransferRequestId",
                table: "AuditLogs",
                column: "TransferRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyTrackingTokens_Token",
                table: "FamilyTrackingTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FamilyTrackingTokens_TransferRequestId",
                table: "FamilyTrackingTokens",
                column: "TransferRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HospitalResponses_BroadcastId",
                table: "HospitalResponses",
                column: "BroadcastId");

            migrationBuilder.CreateIndex(
                name: "IX_HospitalResponses_ReceivingHospitalId",
                table: "HospitalResponses",
                column: "ReceivingHospitalId");

            migrationBuilder.CreateIndex(
                name: "IX_HospitalResponses_RespondingDoctorId",
                table: "HospitalResponses",
                column: "RespondingDoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientRecords_TransferRequestId",
                table: "PatientRecords",
                column: "TransferRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransferRequests_AssignedAmbulanceId",
                table: "TransferRequests",
                column: "AssignedAmbulanceId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferRequests_BroadcastId",
                table: "TransferRequests",
                column: "BroadcastId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferRequests_ReceivingHospitalId",
                table: "TransferRequests",
                column: "ReceivingHospitalId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferRequests_SendingHospitalId",
                table: "TransferRequests",
                column: "SendingHospitalId");

            migrationBuilder.CreateIndex(
                name: "IX_VitalsRecords_TransferRequestId",
                table: "VitalsRecords",
                column: "TransferRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "FamilyTrackingTokens");

            migrationBuilder.DropTable(
                name: "HospitalResponses");

            migrationBuilder.DropTable(
                name: "PatientRecords");

            migrationBuilder.DropTable(
                name: "VitalsRecords");

            migrationBuilder.DropTable(
                name: "TransferRequests");

            migrationBuilder.DropTable(
                name: "AnonymousTransferNeeds");
        }
    }
}
