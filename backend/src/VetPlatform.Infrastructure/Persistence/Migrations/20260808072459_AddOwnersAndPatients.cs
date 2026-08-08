using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnersAndPatients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Owners",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IdentificationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    AlternateContact = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ConsentNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Owners", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Species = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Breed = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EstimatedAge = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Sex = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReproductiveStatus = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    CurrentWeightKg = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    MicrochipNumber = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    PhotoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Allergies = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ChronicDiseases = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CurrentMedications = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    VaccinationStatus = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DewormingStatus = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Patients_Owners_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Owners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PatientWeights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WeightKg = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientWeights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientWeights_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Owners_ClinicId_FullName",
                table: "Owners",
                columns: new[] { "ClinicId", "FullName" });

            migrationBuilder.CreateIndex(
                name: "IX_Owners_ClinicId_IdentificationNumber",
                table: "Owners",
                columns: new[] { "ClinicId", "IdentificationNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_Owners_ClinicId_Phone",
                table: "Owners",
                columns: new[] { "ClinicId", "Phone" });

            migrationBuilder.CreateIndex(
                name: "IX_Patients_ClinicId_MicrochipNumber",
                table: "Patients",
                columns: new[] { "ClinicId", "MicrochipNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_Patients_ClinicId_Name",
                table: "Patients",
                columns: new[] { "ClinicId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Patients_ClinicId_OwnerId",
                table: "Patients",
                columns: new[] { "ClinicId", "OwnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_Patients_OwnerId",
                table: "Patients",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientWeights_ClinicId_PatientId_RecordedAtUtc",
                table: "PatientWeights",
                columns: new[] { "ClinicId", "PatientId", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientWeights_PatientId",
                table: "PatientWeights",
                column: "PatientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PatientWeights");

            migrationBuilder.DropTable(
                name: "Patients");

            migrationBuilder.DropTable(
                name: "Owners");
        }
    }
}
