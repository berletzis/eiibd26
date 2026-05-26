using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eiibd26.Data.Migrations
{
    /// <inheritdoc />
    public partial class DB_06_Indices_FK_Explicitas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Esta migración normaliza el schema al estado actual del modelo EF Core.
            // Las tablas, columnas e índices creados manualmente antes de esta migración
            // (verificados el 2026-05-26 vía INFORMATION_SCHEMA) se omiten para evitar
            // errores de idempotencia. Solo se ejecutan las 3 operaciones genuinamente pendientes:

            // 1. AlterColumn: Texto en EstadoAnimoUsuario estaba en nvarchar(1000), el modelo lo define como 2000
            migrationBuilder.AlterColumn<string>(
                name: "Texto",
                table: "EstadoAnimoUsuario",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            // 2. DB-004: índice PatientId en PatientLaboratoryResults (no existía)
            migrationBuilder.CreateIndex(
                name: "IX_PatientLaboratoryResults_PatientId",
                table: "PatientLaboratoryResults",
                column: "PatientId");

            // 3. DB-015: índice AspNetUserId en MedicosDirectorio (no existía)
            migrationBuilder.CreateIndex(
                name: "IX_MedicosDirectorio_AspNetUserId",
                table: "MedicosDirectorio",
                column: "AspNetUserId",
                filter: "[AspNetUserId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MedicosDirectorio_AspNetUserId",
                table: "MedicosDirectorio");

            migrationBuilder.DropIndex(
                name: "IX_PatientLaboratoryResults_PatientId",
                table: "PatientLaboratoryResults");

            migrationBuilder.AlterColumn<string>(
                name: "Texto",
                table: "EstadoAnimoUsuario",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);
        }
    }
}
