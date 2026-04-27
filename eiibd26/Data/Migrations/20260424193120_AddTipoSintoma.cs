using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace eiibd26.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTipoSintoma : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsPrincipal",
                table: "tratamientoUsuario",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Dolor",
                table: "TrackingSintomaUsuario",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FrecuenciaId",
                table: "TrackingSintomaUsuario",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TieneSangrado",
                table: "TrackingSintomaUsuario",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EsPrincipal",
                table: "sintomasUsuario",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TipoSintoma",
                table: "sintomas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "EsPrincipal",
                table: "condicionUsuario",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "EmailCampanaLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Fase = table.Column<int>(type: "int", nullable: false),
                    TemplateId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaEnvio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Exito = table.Column<bool>(type: "bit", nullable: false),
                    Error = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailCampanaLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FrecuenciaSintomaCatalog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FrecuenciaSintomaCatalog", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "FrecuenciaSintomaCatalog",
                columns: new[] { "Id", "Nombre", "Orden" },
                values: new object[,]
                {
                    { 1, "1 vez al día", 1 },
                    { 2, "2–3 veces al día", 2 },
                    { 3, "4–6 veces al día", 3 },
                    { 4, "Más de 6 veces al día", 4 },
                    { 5, "Ocasional (no diario)", 5 },
                    { 6, "Intermitente", 6 },
                    { 7, "Constante", 7 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrackingSintomaUsuario_FrecuenciaId",
                table: "TrackingSintomaUsuario",
                column: "FrecuenciaId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailCampanaLog_FechaEnvio",
                table: "EmailCampanaLog",
                column: "FechaEnvio");

            migrationBuilder.CreateIndex(
                name: "IX_EmailCampanaLog_UserId_Fase_Exito",
                table: "EmailCampanaLog",
                columns: new[] { "UserId", "Fase", "Exito" });

            migrationBuilder.AddForeignKey(
                name: "FK_TrackingSintomaUsuario_FrecuenciaSintomaCatalog_FrecuenciaId",
                table: "TrackingSintomaUsuario",
                column: "FrecuenciaId",
                principalTable: "FrecuenciaSintomaCatalog",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrackingSintomaUsuario_FrecuenciaSintomaCatalog_FrecuenciaId",
                table: "TrackingSintomaUsuario");

            migrationBuilder.DropTable(
                name: "EmailCampanaLog");

            migrationBuilder.DropTable(
                name: "FrecuenciaSintomaCatalog");

            migrationBuilder.DropIndex(
                name: "IX_TrackingSintomaUsuario_FrecuenciaId",
                table: "TrackingSintomaUsuario");

            migrationBuilder.DropColumn(
                name: "EsPrincipal",
                table: "tratamientoUsuario");

            migrationBuilder.DropColumn(
                name: "Dolor",
                table: "TrackingSintomaUsuario");

            migrationBuilder.DropColumn(
                name: "FrecuenciaId",
                table: "TrackingSintomaUsuario");

            migrationBuilder.DropColumn(
                name: "TieneSangrado",
                table: "TrackingSintomaUsuario");

            migrationBuilder.DropColumn(
                name: "EsPrincipal",
                table: "sintomasUsuario");

            migrationBuilder.DropColumn(
                name: "TipoSintoma",
                table: "sintomas");

            migrationBuilder.DropColumn(
                name: "EsPrincipal",
                table: "condicionUsuario");
        }
    }
}
